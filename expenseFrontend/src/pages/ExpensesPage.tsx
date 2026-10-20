import { useEffect, useState } from "react";
import { ApiError, deleteExpense, getCategories, listExpenses } from "../api";
import ConfirmDialog from "../components/ConfirmDialog";
import ExpenseFilters, { type Filters } from "../components/ExpenseFilters";
import ExpenseForm from "../components/ExpenseForm";
import { formatCurrency } from "../format";
import type { Category, Expense, ExpenseListQuery, PagedResult } from "../types";

const PAGE_SIZE = 20;

type Result = {
  query: ExpenseListQuery;
  data?: PagedResult<Expense>;
  error?: string;
};

function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return Object.values(error.fieldErrors).flat()[0] ?? error.message;
  }
  return "Something went wrong.";
}

function ExpensesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [filters, setFilters] = useState<Filters>({ startDate: "", endDate: "", categoryId: "" });
  const [query, setQuery] = useState<ExpenseListQuery>({ page: 1, pageSize: PAGE_SIZE });
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

  function changeFilters(next: Filters) {
    setFilters(next);
    setQuery({ ...next, page: 1, pageSize: PAGE_SIZE });
  }

  function goToPage(page: number) {
    setQuery({ ...query, page });
  }

  function handleSaved() {
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
      const lastOnPage = data?.items.length === 1 && (query.page ?? 1) > 1;
      setQuery({ ...query, page: lastOnPage ? (query.page ?? 1) - 1 : query.page });
    } catch (error) {
      setActionError(errorMessage(error));
    }
  }

  const loading = result?.query !== query;
  const data = result?.data;
  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / data.pageSize)) : 1;

  return (
    <>
      <div className="page-title">
        <h1>Expenses</h1>
        {editing === null && (
          <button type="button" onClick={() => setEditing("new")}>
            New expense
          </button>
        )}
      </div>

      {editing !== null && (
        <ExpenseForm
          key={editing === "new" ? "new" : editing.id}
          expense={editing === "new" ? undefined : editing}
          categories={categories}
          onSaved={handleSaved}
          onCancel={() => setEditing(null)}
        />
      )}

      {toDelete && (
        <ConfirmDialog
          message={`Delete this expense of ${formatCurrency(toDelete.amount, toDelete.currency)}?`}
          onConfirm={confirmDelete}
          onCancel={() => setToDelete(null)}
        />
      )}

      {actionError && (
        <p className="error" role="alert">
          {actionError}
        </p>
      )}

      <ExpenseFilters filters={filters} categories={categories} onChange={changeFilters} />

      {loading && <p>Loading expenses...</p>}

      {!loading && result?.error && (
        <p className="error" role="alert">
          {result.error}
        </p>
      )}

      {!loading && data && data.items.length === 0 && <p>No expenses found.</p>}

      {!loading && data && data.items.length > 0 && (
        <>
          <table className="expenses">
            <thead>
              <tr>
                <th>Date</th>
                <th>Merchant / description</th>
                <th>Category</th>
                <th className="amount">Amount</th>
                <th>Currency</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {data.items.map((e) => (
                <tr key={e.id}>
                  <td>{e.expenseDate}</td>
                  <td>
                    {e.merchant ?? "—"}
                    {e.description && <div className="muted">{e.description}</div>}
                  </td>
                  <td>{e.categoryName}</td>
                  <td className="amount">{formatCurrency(e.amount, e.currency)}</td>
                  <td>{e.currency}</td>
                  <td className="row-actions">
                    <button type="button" onClick={() => setEditing(e)}>
                      Edit
                    </button>
                    <button type="button" onClick={() => setToDelete(e)}>
                      Delete
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>

          <div className="pagination">
            <button type="button" disabled={data.page <= 1} onClick={() => goToPage(data.page - 1)}>
              Previous
            </button>
            <span>
              Page {data.page} of {totalPages} ({data.totalCount} expenses)
            </span>
            <button
              type="button"
              disabled={data.page >= totalPages}
              onClick={() => goToPage(data.page + 1)}
            >
              Next
            </button>
          </div>
        </>
      )}
    </>
  );
}

export default ExpensesPage;
