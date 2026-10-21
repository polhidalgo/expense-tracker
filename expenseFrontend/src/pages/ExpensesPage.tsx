import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { createExpense, deleteExpense, errorMessage, getCategories, listExpenses, updateExpense } from "../api";
import Alert from "../components/Alert";
import ConfirmDialog from "../components/ConfirmDialog";
import EmptyState from "../components/EmptyState";
import ExpenseFilters, { type Filters } from "../components/ExpenseFilters";
import ExpenseForm from "../components/ExpenseForm";
import Sheet from "../components/Sheet";
import { TableSkeleton } from "../components/Skeleton";
import { categoryColor } from "../categoryColors";
import { formatCurrency, formatDate } from "../format";
import RowMenu from "../components/RowMenu";
import { ChevronLeftIcon, ChevronRightIcon } from "../icons";
import { useToast } from "../toast";
import type { Category, Expense, ExpenseListQuery, PagedResult } from "../types";
import { usePageTitle } from "../usePageTitle";

const PAGE_SIZE = 20;

type Result = {
  query: ExpenseListQuery;
  data?: PagedResult<Expense>;
  error?: string;
};

const filtersFrom = (params: URLSearchParams): Filters => ({
  startDate: params.get("startDate") ?? "",
  endDate: params.get("endDate") ?? "",
  categoryId: params.get("categoryId") ?? "",
});

// Only the filters that are set, e.g. "categoryId=…&startDate=…".
const searchFrom = (filters: Filters) =>
  new URLSearchParams(Object.entries(filters).filter(([, value]) => value)).toString();

// The merchant, else the description, else "Untitled".
const primaryText = (e: Expense) => e.merchant ?? e.description ?? "Untitled";

function ExpensesPage() {
  usePageTitle("Expenses");
  const showToast = useToast();
  const [categories, setCategories] = useState<Category[]>([]);
  // The filters live in the URL, so a category row on This week (T19), reload and the nav link agree.
  const [searchParams, setSearchParams] = useSearchParams();
  const search = searchParams.toString();
  const [filters, setFilters] = useState<Filters>(() => filtersFrom(searchParams));
  const [query, setQuery] = useState<ExpenseListQuery>(() => ({ ...filters, page: 1, pageSize: PAGE_SIZE }));
  const [seenSearch, setSeenSearch] = useState(search);
  const [result, setResult] = useState<Result>();
  const [editing, setEditing] = useState<Expense | "new" | null>(null);
  const [toDelete, setToDelete] = useState<Expense | null>(null);
  const [actionError, setActionError] = useState("");

  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch(() => setCategories([]));
  }, []);

  useEffect(() => {
    let ignore = false;
    listExpenses(query)
      .then((data) => !ignore && setResult({ query, data }))
      .catch((error) => !ignore && setResult({ query, error: errorMessage(error) }));
    return () => {
      ignore = true;
    };
  }, [query]);

  // The URL changed from outside (e.g. the "Expenses" nav link): its filters win, back on page 1.
  if (search !== seenSearch) {
    const next = filtersFrom(searchParams);
    setSeenSearch(search);
    setFilters(next);
    setQuery({ ...next, page: 1, pageSize: PAGE_SIZE });
  }

  function changeFilters(next: Filters) {
    const nextSearch = searchFrom(next);
    setFilters(next);
    setQuery({ ...next, page: 1, pageSize: PAGE_SIZE });
    setSeenSearch(nextSearch);
    setSearchParams(nextSearch, { replace: true });
  }

  function goToPage(page: number) {
    setQuery({ ...query, page });
  }

  function handleSaved() {
    showToast(editing === "new" ? "Expense added" : "Changes saved");
    setEditing(null);
    setQuery({ ...query });
  }

  async function confirmDelete() {
    if (!toDelete) return;
    const expense = toDelete;
    setToDelete(null);
    setActionError("");
    try {
      await deleteExpense(expense.id);
      showToast("Expense deleted");
      const lastOnPage = data?.items.length === 1 && (query.page ?? 1) > 1;
      setQuery({ ...query, page: lastOnPage ? (query.page ?? 1) - 1 : query.page });
    } catch (error) {
      setActionError(errorMessage(error));
    }
  }

  // The previous result stays on screen (dimmed) until the new one arrives.
  const loading = result?.query !== query;
  const data = result?.data;
  const filtered = Boolean(filters.startDate || filters.endDate || filters.categoryId);
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;
  const firstItem = data ? (data.page - 1) * data.pageSize + 1 : 0;
  const lastItem = data ? firstItem + data.items.length - 1 : 0;

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Expenses</h1>
          {data && (
            <p className="muted">
              {data.totalCount} {data.totalCount === 1 ? "expense" : "expenses"}
              {filtered && " · filtered"}
            </p>
          )}
        </div>
        <div className="actions">
          <Link className="btn btn--secondary" to="/receipt">
            Scan receipt
          </Link>
          <button
            className="btn btn--primary"
            type="button"
            disabled={editing !== null}
            onClick={() => setEditing("new")}
          >
            Add expense
          </button>
        </div>
      </div>

      {editing !== null && (
        <Sheet
          label={editing === "new" ? "Add expense" : "Edit expense"}
          guardChanges
          onClose={() => setEditing(null)}
        >
          {(requestClose) => editing === "new" ? (
            <ExpenseForm
              key="new"
              title="Add expense"
              submitLabel="Save expense"
              categories={categories}
              onSubmit={createExpense}
              onSaved={handleSaved}
              onCancel={requestClose}
            />
          ) : (
            <ExpenseForm
              key={editing.id}
              title="Edit expense"
              submitLabel="Save changes"
              categories={categories}
              initial={{
                amount: String(editing.amount),
                currency: editing.currency,
                expenseDate: editing.expenseDate,
                categoryId: editing.categoryId,
                merchant: editing.merchant ?? "",
                description: editing.description ?? "",
              }}
              onSubmit={(body) => updateExpense(editing.id, body)}
              onSaved={handleSaved}
              onCancel={requestClose}
            />
          )}
        </Sheet>
      )}

      {toDelete && (
        <ConfirmDialog
          title="Delete expense?"
          message={`${primaryText(toDelete)}, ${formatCurrency(toDelete.amount, toDelete.currency)} on ${formatDate(toDelete.expenseDate)}. This can't be undone.`}
          onConfirm={confirmDelete}
          onCancel={() => setToDelete(null)}
        />
      )}

      <ExpenseFilters filters={filters} categories={categories} onChange={changeFilters} />

      {actionError && <Alert>{actionError}</Alert>}

      {!result && <TableSkeleton />}

      {!loading && result?.error && <Alert>{result.error}</Alert>}

      {data && data.items.length === 0 && !loading && (
        filtered ? (
          <EmptyState
            title="No expenses match these filters"
            actions={
              <button
                className="btn btn--secondary"
                type="button"
                onClick={() => changeFilters({ startDate: "", endDate: "", categoryId: "" })}
              >
                Clear filters
              </button>
            }
          />
        ) : (
          <EmptyState
            title="No expenses yet"
            description="Add your first expense or scan a receipt."
            actions={
              <>
                <button className="btn btn--primary" type="button" onClick={() => setEditing("new")}>
                  Add expense
                </button>
                <Link className="btn btn--secondary" to="/receipt">
                  Scan receipt
                </Link>
              </>
            }
          />
        )
      )}

      {data && data.items.length > 0 && (
        <div className={loading ? "stack is-refreshing" : "stack"} aria-busy={loading}>
          <div className="table-wrap">
            <table className="data-table data-table--expenses">
              <thead>
                <tr>
                  <th className="col-date">Date</th>
                  <th>Description</th>
                  <th className="col-category">Category</th>
                  <th className="amount col-amount">Amount</th>
                  <th className="col-actions">
                    <span className="sr-only">Actions</span>
                  </th>
                </tr>
              </thead>
              <tbody>
                {data.items.map((e) => {
                  const isEditing = editing !== null && editing !== "new" && editing.id === e.id;
                  return (
                    <tr key={e.id} className={isEditing ? "is-editing" : undefined} aria-current={isEditing || undefined}>
                      <td className="muted c-date">
                        <time dateTime={e.expenseDate}>{formatDate(e.expenseDate)}</time>
                      </td>
                      <td className="c-main">
                        <div className="cell-primary">{primaryText(e)}</div>
                        {e.merchant && e.description && (
                          <div className="cell-secondary" title={e.description}>
                            {e.description}
                          </div>
                        )}
                      </td>
                      <td className="c-category">
                        <span className="swatch" style={{ background: categoryColor(e.categoryId) }} />
                        {e.categoryName}
                      </td>
                      <td className="amount c-amount">{formatCurrency(e.amount, e.currency)}</td>
                      <td className="c-actions">
                        <RowMenu label={primaryText(e)} onEdit={() => setEditing(e)} onDelete={() => setToDelete(e)} />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          <div className="pagination">
            <span className="muted num">
              {firstItem}–{lastItem} of {data.totalCount}
            </span>
            <div className="actions">
              <button
                className="btn btn--secondary btn--sm"
                type="button"
                disabled={data.page <= 1}
                onClick={() => goToPage(data.page - 1)}
              >
                <ChevronLeftIcon />
                Previous
              </button>
              <button
                className="btn btn--secondary btn--sm"
                type="button"
                disabled={data.page >= totalPages}
                onClick={() => goToPage(data.page + 1)}
              >
                Next
                <ChevronRightIcon />
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

export default ExpensesPage;
