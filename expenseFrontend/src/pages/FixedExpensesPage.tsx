import { useEffect, useId, useState, type FormEvent } from "react";
import {
  ApiError,
  createFixedExpense,
  deleteFixedExpense,
  errorMessage,
  getCategories,
  listFixedExpenses,
  updateFixedExpense,
} from "../api";
import Alert from "../components/Alert";
import AmountInput from "../components/AmountInput";
import CategorySelect from "../components/CategorySelect";
import ConfirmDialog from "../components/ConfirmDialog";
import EmptyState from "../components/EmptyState";
import Field from "../components/Field";
import RowMenu from "../components/RowMenu";
import Sheet from "../components/Sheet";
import { TableSkeleton } from "../components/Skeleton";
import { categoryColor } from "../categoryColors";
import { formatCurrency, formatWeekdayDate, toIsoDate } from "../format";
import { useToast } from "../toast";
import type { Category, FieldErrors, FixedExpense, FixedFrequency } from "../types";
import { usePageTitle } from "../usePageTitle";

const FREQUENCIES: { value: FixedFrequency; label: string }[] = [
  { value: "Weekly", label: "Weekly" },
  { value: "Fortnightly", label: "Every 2 weeks" },
  { value: "Monthly", label: "Monthly" },
];

type FormValues = {
  name: string;
  amount: string;
  categoryId: string;
  startDate: string;
  frequency: FixedFrequency;
};

function ordinal(day: number): string {
  if (day >= 11 && day <= 13) return `${day}th`;
  const suffixes: Record<number, string> = { 1: "st", 2: "nd", 3: "rd" };
  const suffix = suffixes[day % 10] ?? "th";
  return `${day}${suffix}`;
}

// "Weekly", "Every 2 weeks" or "Monthly on the 31st".
function repeatsLabel(f: FixedExpense): string {
  if (f.frequency === "Weekly") return "Weekly";
  if (f.frequency === "Fortnightly") return "Every 2 weeks";
  return `Monthly on the ${ordinal(Number(f.startDate.slice(8, 10)))}`;
}

// Calendar-day arithmetic in UTC, so daylight saving never shifts a date.
const dayNumber = (iso: string) => {
  const [y, m, d] = iso.split("-").map(Number);
  return Date.UTC(y, m - 1, d) / 86_400_000;
};

const fromDayNumber = (n: number) => new Date(n * 86_400_000).toISOString().slice(0, 10);

// The same day in a later month, or that month's last day (D-23).
function monthlyDate(startIso: string, monthsLater: number): string {
  const [y, m, d] = startIso.split("-").map(Number);
  const lastDay = new Date(Date.UTC(y, m - 1 + monthsLater + 1, 0)).getUTCDate();
  return new Date(Date.UTC(y, m - 1 + monthsLater, Math.min(d, lastDay))).toISOString().slice(0, 10);
}

// Next due date on or after today. For display only; the server calculates the real payments.
function nextPayment(f: FixedExpense, todayIso: string): string {
  if (f.startDate >= todayIso) return f.startDate;
  if (f.frequency === "Monthly") {
    const [sy, sm] = f.startDate.split("-").map(Number);
    const [ty, tm] = todayIso.split("-").map(Number);
    const months = (ty - sy) * 12 + (tm - sm);
    const candidate = monthlyDate(f.startDate, months);
    return candidate >= todayIso ? candidate : monthlyDate(f.startDate, months + 1);
  }
  const step = f.frequency === "Weekly" ? 7 : 14;
  const start = dayNumber(f.startDate);
  const steps = Math.ceil((dayNumber(todayIso) - start) / step);
  return fromDayNumber(start + steps * step);
}

function FixedExpensesPage() {
  usePageTitle("Fixed expenses");
  const showToast = useToast();
  const [categories, setCategories] = useState<Category[]>([]);
  const [items, setItems] = useState<FixedExpense[]>();
  const [loadError, setLoadError] = useState("");
  const [editing, setEditing] = useState<FixedExpense | "new" | null>(null);
  const [toDelete, setToDelete] = useState<FixedExpense | null>(null);
  const [actionError, setActionError] = useState("");
  const today = toIsoDate(new Date());

  function load() {
    listFixedExpenses()
      .then((data) => {
        setItems(data);
        setLoadError("");
      })
      .catch((error) => setLoadError(errorMessage(error)));
  }

  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch(() => setCategories([]));
    load();
  }, []);

  function handleSaved() {
    showToast(editing === "new" ? "Fixed expense added" : "Changes saved");
    setEditing(null);
    load();
  }

  async function confirmDelete() {
    if (!toDelete) return;
    const fixedExpense = toDelete;
    setToDelete(null);
    setActionError("");
    try {
      await deleteFixedExpense(fixedExpense.id);
      showToast("Fixed expense deleted");
      load();
    } catch (error) {
      setActionError(errorMessage(error));
    }
  }

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Fixed expenses</h1>
          {items && (
            <p className="muted">
              {items.length} {items.length === 1 ? "fixed expense" : "fixed expenses"}
            </p>
          )}
        </div>
        <button
          className="btn btn--primary"
          type="button"
          disabled={editing !== null}
          onClick={() => setEditing("new")}
        >
          Add fixed expense
        </button>
      </div>

      <p className="muted">Their payments count automatically on their due dates. Don't also add them as expenses.</p>

      {editing !== null && (
        <Sheet
          label={editing === "new" ? "Add fixed expense" : "Edit fixed expense"}
          guardChanges
          onClose={() => setEditing(null)}
        >
          {(requestClose) => (
          <FixedExpenseForm
            key={editing === "new" ? "new" : editing.id}
            categories={categories}
            editing={editing === "new" ? null : editing}
            onSaved={handleSaved}
            onCancel={requestClose}
          />
          )}
        </Sheet>
      )}

      {toDelete && (
        <ConfirmDialog
          title="Delete fixed expense?"
          message={`${toDelete.name}, ${formatCurrency(toDelete.amount, toDelete.currency)} ${repeatsLabel(
            toDelete,
          ).toLowerCase()}. Its payments disappear from every week and month, past ones included.`}
          onConfirm={confirmDelete}
          onCancel={() => setToDelete(null)}
        />
      )}

      {actionError && <Alert>{actionError}</Alert>}
      {loadError && <Alert>{loadError}</Alert>}

      {!items && !loadError && <TableSkeleton />}

      {items && items.length === 0 && (
        <EmptyState
          title="No fixed expenses"
          description="Add rent, bills or subscriptions that repeat."
          actions={
            <button className="btn btn--primary" type="button" onClick={() => setEditing("new")}>
              Add fixed expense
            </button>
          }
        />
      )}

      {items && items.length > 0 && (
        <div className="table-wrap">
          <table className="data-table data-table--fixed">
            <thead>
              <tr>
                <th>Name</th>
                <th className="col-category">Category</th>
                <th>Repeats</th>
                <th>Next payment</th>
                <th className="amount col-amount">Amount</th>
                <th className="col-actions">
                  <span className="sr-only">Actions</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {items.map((f) => {
                const isEditing = editing !== null && editing !== "new" && editing.id === f.id;
                const next = nextPayment(f, today);
                return (
                  <tr key={f.id} className={isEditing ? "is-editing" : undefined} aria-current={isEditing || undefined}>
                    <td className="cell-primary c-main">{f.name}</td>
                    <td className="c-category">
                      <span className="swatch" style={{ background: categoryColor(f.categoryId) }} />
                      {f.categoryName}
                    </td>
                    <td className="c-repeats">{repeatsLabel(f)}</td>
                    <td className="muted c-date">
                      <time dateTime={next}>{formatWeekdayDate(next)}</time>
                    </td>
                    <td className="amount c-amount">{formatCurrency(f.amount, f.currency)}</td>
                    <td className="c-actions">
                      <RowMenu label={f.name} onEdit={() => setEditing(f)} onDelete={() => setToDelete(f)} />
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}

type FormProps = {
  categories: Category[];
  editing: FixedExpense | null;
  onSaved: () => void;
  onCancel: () => void;
};

function FixedExpenseForm({ categories, editing, onSaved, onCancel }: FormProps) {
  const [values, setValues] = useState<FormValues>(() =>
    editing
      ? {
          name: editing.name,
          amount: String(editing.amount),
          categoryId: editing.categoryId,
          startDate: editing.startDate,
          frequency: editing.frequency,
        }
      : { name: "", amount: "", categoryId: "", startDate: toIsoDate(new Date()), frequency: "Monthly" },
  );
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const id = useId();

  const set = (field: keyof FormValues, value: string) => setValues({ ...values, [field]: value });

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (saving) return;
    setSaving(true);
    setErrors({});
    setMessage("");

    const body = { ...values, amount: Number(values.amount) };
    try {
      await (editing ? updateFixedExpense(editing.id, body) : createFixedExpense(body));
      onSaved();
    } catch (error) {
      if (error instanceof ApiError) {
        setErrors(error.fieldErrors);
        if (Object.keys(error.fieldErrors).length === 0) setMessage(error.message);
      } else {
        setMessage("Something went wrong.");
      }
    } finally {
      setSaving(false);
    }
  }

  const errorOf = (name: keyof FormValues) => errors[name]?.[0] ?? errors[`$.${name}`]?.[0];
  const control = (name: keyof FormValues, hasHelper = false) => ({
    id: `${id}-${name}`,
    "aria-invalid": Boolean(errorOf(name)),
    "aria-describedby": errorOf(name) || hasHelper ? `${id}-${name}-msg` : undefined,
  });
  const monthly = values.frequency === "Monthly";

  return (
    <form className="form" onSubmit={handleSubmit}>
      <h2 className="form__title">{editing ? "Edit fixed expense" : "Add fixed expense"}</h2>

      <div className="form__body">
        {message && <Alert>{message}</Alert>}

        <Field id={`${id}-name`} label="Name" error={errorOf("name")}>
          <input
            {...control("name")}
            required
            maxLength={150}
            autoFocus={!editing}
            value={values.name}
            onChange={(e) => set("name", e.target.value)}
          />
        </Field>

        <Field id={`${id}-amount`} label="Amount" error={errorOf("amount")}>
          <AmountInput
            {...control("amount")}
            large
            required
            value={values.amount}
            onChange={(e) => set("amount", e.target.value)}
          />
        </Field>

        <Field id={`${id}-categoryId`} label="Category" error={errorOf("categoryId")}>
          <CategorySelect
            {...control("categoryId")}
            required
            value={values.categoryId}
            categories={categories}
            emptyLabel="Select a category"
            onChange={(value) => set("categoryId", value)}
          />
        </Field>

        <Field id={`${id}-startDate`} label="Start date" error={errorOf("startDate")}>
          <input
            {...control("startDate")}
            type="date"
            required
            value={values.startDate}
            onChange={(e) => set("startDate", e.target.value)}
          />
        </Field>

        <Field
          id={`${id}-frequency`}
          label="Repeats"
          error={errorOf("frequency")}
          helper={monthly ? "On days a month doesn't have (e.g. the 31st), the payment falls on the last day." : undefined}
        >
          <select
            {...control("frequency", monthly)}
            value={values.frequency}
            onChange={(e) => set("frequency", e.target.value)}
          >
            {FREQUENCIES.map((f) => (
              <option key={f.value} value={f.value}>
                {f.label}
              </option>
            ))}
          </select>
        </Field>
      </div>

      <div className="form__footer">
        <button className="btn btn--secondary" type="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
        <button className="btn btn--primary" type="submit" disabled={saving}>
          {saving ? "Saving…" : editing ? "Save changes" : "Save fixed expense"}
        </button>
      </div>
    </form>
  );
}

export default FixedExpensesPage;
