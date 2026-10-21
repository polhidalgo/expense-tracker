import { useEffect, useId, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { ApiError, deleteBudget, errorMessage, getWeekSummary, listExpenses, setBudget } from "../api";
import Alert from "../components/Alert";
import AmountInput from "../components/AmountInput";
import CategoryDonut from "../components/CategoryDonut";
import ConfirmDialog from "../components/ConfirmDialog";
import EmptyState from "../components/EmptyState";
import Field from "../components/Field";
import { MetricSkeleton, TableSkeleton } from "../components/Skeleton";
import { categoryColor } from "../categoryColors";
import {
  addDays,
  formatCurrency,
  formatDate,
  formatDateRange,
  formatWeekdayDate,
  mondayOf,
  toIsoDate,
} from "../format";
import { ChevronLeftIcon, ChevronRightIcon } from "../icons";
import type { Expense, WeekSummary } from "../types";
import { usePageTitle } from "../usePageTitle";

// Enough for one week; "View all expenses" covers the rest.
const WEEK_EXPENSES_PAGE_SIZE = 100;

type Result = {
  date: string;
  summary?: WeekSummary;
  expenses?: Expense[];
  expenseCount?: number;
  error?: string;
};

const aud = (amount: number) => formatCurrency(amount, "AUD");
const plural = (n: number, one: string, many: string) => `${n} ${n === 1 ? one : many}`;

function WeekPage() {
  // The frontend sends its own "today", so the server never guesses the timezone.
  const [date, setDate] = useState(() => toIsoDate(new Date()));
  const [reload, setReload] = useState(0);
  const [result, setResult] = useState<Result>();
  const [editingBudget, setEditingBudget] = useState(false);

  const thisMonday = mondayOf(toIsoDate(new Date()));
  const shownMonday = mondayOf(date);
  const isCurrentWeek = shownMonday === thisMonday;
  const title = isCurrentWeek ? "This week" : `Week of ${formatDate(shownMonday)}`;
  usePageTitle(title);

  useEffect(() => {
    let ignore = false;
    getWeekSummary(date)
      .then(async (summary) => {
        const expenses = await listExpenses({
          startDate: summary.weekStart,
          endDate: summary.weekEnd,
          pageSize: WEEK_EXPENSES_PAGE_SIZE,
        });
        if (!ignore) setResult({ date, summary, expenses: expenses.items, expenseCount: expenses.totalCount });
      })
      .catch((error) => !ignore && setResult({ date, error: errorMessage(error) }));
    return () => {
      ignore = true;
    };
  }, [date, reload]);

  function handleBudgetSaved() {
    setEditingBudget(false);
    setReload((n) => n + 1);
  }

  const summary = result?.summary;
  // The previous week stays on screen (dimmed) until the new one arrives.
  const loading = result?.date !== date;
  const range = formatDateRange(shownMonday, addDays(shownMonday, 6));

  return (
    <>
      <div className="page-header">
        <h1>{title}</h1>
        <div className="week-nav">
          <button
            className="btn btn--ghost btn--icon"
            type="button"
            aria-label="Previous week"
            onClick={() => setDate(addDays(date, -7))}
          >
            <ChevronLeftIcon />
          </button>
          <span className="week-nav__range num">{range}</span>
          <button
            className="btn btn--ghost btn--icon"
            type="button"
            aria-label="Next week"
            onClick={() => setDate(addDays(date, 7))}
          >
            <ChevronRightIcon />
          </button>
          {!isCurrentWeek && (
            <button className="btn btn--secondary btn--sm" type="button" onClick={() => setDate(toIsoDate(new Date()))}>
              This week
            </button>
          )}
        </div>
      </div>

      {!result && (
        <>
          <MetricSkeleton />
          <TableSkeleton />
        </>
      )}

      {!loading && result?.error && <Alert>{result.error}</Alert>}

      {summary && (
        <div className={loading ? "stack is-refreshing" : "stack"} aria-busy={loading}>
          <section className="metric" aria-label="Budget">
            {editingBudget ? (
              <BudgetForm
                current={summary.weeklyBudget}
                onSaved={handleBudgetSaved}
                onCancel={() => setEditingBudget(false)}
              />
            ) : (
              <BudgetSummary
                summary={summary}
                expenseCount={result?.expenseCount ?? 0}
                onEdit={() => setEditingBudget(true)}
              />
            )}
          </section>

          {summary.byCategory.length === 0 ? (
            <EmptyState
              title="Nothing spent this week"
              description={range}
              actions={
                <Link className="btn btn--primary" to="/expenses">
                  Add expense
                </Link>
              }
            />
          ) : (
            <div className="week-grid">
              <section className="section">
                <h2>Spending by category</h2>
                <CategoryDonut
                  byCategory={summary.byCategory}
                  spent={summary.spent}
                  weekStart={summary.weekStart}
                  weekEnd={summary.weekEnd}
                />
              </section>

              <div className="stack">
                <section className="section">
                  <h2>Fixed payments</h2>
                  {summary.fixedPayments.length === 0 ? (
                    <p className="muted">No fixed payments due this week.</p>
                  ) : (
                    <ul className="compact-list">
                      {summary.fixedPayments.map((p) => (
                        <li key={`${p.fixedExpenseId}-${p.date}`}>
                          <time className="muted" dateTime={p.date}>
                            {formatWeekdayDate(p.date)}
                          </time>
                          <span className="compact-list__name">{p.name}</span>
                          <span className="amount">{aud(p.amount)}</span>
                        </li>
                      ))}
                    </ul>
                  )}
                </section>

                <section className="section">
                  <h2>Expenses</h2>
                  {result?.expenses?.length === 0 ? (
                    <p className="muted">No expenses this week.</p>
                  ) : (
                    <ul className="compact-list">
                      {result?.expenses?.map((e) => (
                        <li key={e.id}>
                          <time className="muted" dateTime={e.expenseDate}>
                            {formatWeekdayDate(e.expenseDate)}
                          </time>
                          <span className="compact-list__name">
                            <span className="swatch" style={{ background: categoryColor(e.categoryId) }} />
                            {e.merchant ?? e.description ?? "Untitled"}
                          </span>
                          <span className="amount">{formatCurrency(e.amount, e.currency)}</span>
                        </li>
                      ))}
                    </ul>
                  )}
                  <Link to="/expenses">View all expenses</Link>
                </section>
              </div>
            </div>
          )}
        </div>
      )}
    </>
  );
}

type BudgetSummaryProps = {
  summary: WeekSummary;
  expenseCount: number;
  onEdit: () => void;
};

// Only the figures the API returns (§C.4).
function BudgetSummary({ summary, expenseCount, onEdit }: BudgetSummaryProps) {
  const caption = `Spent ${aud(summary.spent)} · ${plural(expenseCount, "expense", "expenses")} · ${plural(
    summary.fixedPayments.length,
    "fixed payment",
    "fixed payments",
  )}`;

  if (summary.weeklyBudget === null) {
    return (
      <>
        <p className="metric__value num">{aud(summary.spent)} spent</p>
        <div className="actions">
          <span className="muted">Set a weekly budget to see what's left</span>
          <button className="btn btn--secondary btn--sm" type="button" onClick={onEdit}>
            Set budget
          </button>
        </div>
        <p className="muted">{caption}</p>
      </>
    );
  }

  const remaining = summary.remaining ?? 0;
  const percentLeft = summary.percentLeft ?? 0;
  const over = remaining < 0;

  return (
    <>
      <div className="metric__top">
        <p className={over ? "metric__value num is-over" : "metric__value num"}>
          {over ? `${aud(-remaining)} over` : `${aud(remaining)} left`}
          {over && <span className="badge badge--over">Over</span>}
        </p>
        <div className="actions">
          <span className="muted">Budget {aud(summary.weeklyBudget)}</span>
          <button className="btn btn--ghost btn--sm" type="button" onClick={onEdit}>
            Edit
          </button>
        </div>
      </div>
      <div className="metric__bar">
        <div className={over ? "budget-bar is-over" : "budget-bar"} aria-hidden="true">
          <div className="budget-bar__fill" style={{ width: `${percentLeft}%` }} />
        </div>
        <span className="small num">{percentLeft}% left</span>
      </div>
      <p className="muted">{caption}</p>
    </>
  );
}

type BudgetFormProps = {
  current: number | null;
  onSaved: () => void;
  onCancel: () => void;
};

// Inline, in place of the budget summary (§C.15).
function BudgetForm({ current, onSaved, onCancel }: BudgetFormProps) {
  const [amount, setAmount] = useState(current === null ? "" : String(current));
  const [error, setError] = useState("");
  const [saving, setSaving] = useState(false);
  const [confirmRemove, setConfirmRemove] = useState(false);
  const id = useId();

  async function run(action: () => Promise<unknown>) {
    if (saving) return;
    setSaving(true);
    setError("");
    try {
      await action();
      onSaved();
    } catch (e) {
      setError(e instanceof ApiError ? errorMessage(e) : "Something went wrong.");
    } finally {
      setSaving(false);
    }
  }

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    run(() => setBudget(Number(amount)));
  }

  return (
    <form className="budget-form" onSubmit={handleSubmit}>
      <Field
        id={id}
        label="Weekly budget"
        error={error}
        helper="Used for every week; the monthly budget is calculated from it."
      >
        <AmountInput
          id={id}
          required
          autoFocus
          aria-invalid={Boolean(error)}
          aria-describedby={`${id}-msg`}
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />
      </Field>
      <div className="actions">
        {current !== null && (
          <button
            className="btn btn--ghost btn--sm btn--danger-text"
            type="button"
            onClick={() => setConfirmRemove(true)}
            disabled={saving}
          >
            Remove budget
          </button>
        )}
        <span className="actions__spacer" />
        <button className="btn btn--ghost btn--sm" type="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
        <button className="btn btn--primary btn--sm" type="submit" disabled={saving}>
          {saving ? "Saving…" : "Save"}
        </button>
      </div>

      {confirmRemove && (
        <ConfirmDialog
          title="Remove weekly budget?"
          message="Weeks and months will show spending only."
          confirmLabel="Remove budget"
          onConfirm={() => {
            setConfirmRemove(false);
            run(deleteBudget);
          }}
          onCancel={() => setConfirmRemove(false)}
        />
      )}
    </form>
  );
}

export default WeekPage;
