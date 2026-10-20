import { useRef, useState, type FormEvent } from "react";
import { ApiError } from "../api";
import { toIsoDate } from "../format";
import type { Category, ExpenseRequest, FieldErrors } from "../types";

export type ExpenseFormValues = {
  amount: string;
  currency: string;
  expenseDate: string;
  categoryId: string;
  merchant: string;
  description: string;
};

type Props = {
  title: string;
  categories: Category[];
  initial?: ExpenseFormValues;
  notDetected?: (keyof ExpenseFormValues)[];
  onSubmit: (body: ExpenseRequest) => Promise<unknown>;
  onSaved: () => void;
  onCancel: () => void;
};

const emptyValues = (): ExpenseFormValues => ({
  amount: "",
  currency: "AUD",
  expenseDate: toIsoDate(new Date()),
  categoryId: "",
  merchant: "",
  description: "",
});

function ExpenseForm({ title, categories, initial, notDetected = [], onSubmit, onSaved, onCancel }: Props) {
  const [values, setValues] = useState<ExpenseFormValues>(initial ?? emptyValues);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);

  const set = (field: keyof ExpenseFormValues, value: string) => setValues({ ...values, [field]: value });

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (savingRef.current) return;
    savingRef.current = true;
    setSaving(true);
    setErrors({});
    setMessage("");

    try {
      await onSubmit({
        amount: Number(values.amount),
        currency: values.currency || undefined,
        expenseDate: values.expenseDate,
        categoryId: values.categoryId,
        merchant: values.merchant || undefined,
        description: values.description || undefined,
      });
      onSaved();
    } catch (error) {
      if (error instanceof ApiError) {
        setErrors(error.fieldErrors);
        if (Object.keys(error.fieldErrors).length === 0) setMessage(error.message);
      } else {
        setMessage("Something went wrong.");
      }
    } finally {
      savingRef.current = false;
      setSaving(false);
    }
  }

  // Shows the backend error for the field, or "Not detected" for an empty AI suggestion.
  const fieldNote = (name: keyof ExpenseFormValues) => {
    const text = errors[name]?.[0] ?? errors[`$.${name}`]?.[0];
    if (text) return <span className="error">{text}</span>;
    if (notDetected.includes(name)) return <span className="muted">Not detected</span>;
    return null;
  };

  return (
    <form className="expense-form" onSubmit={handleSubmit}>
      <h2>{title}</h2>

      <label>
        Amount
        <input
          type="number"
          step="any"
          required
          value={values.amount}
          onChange={(e) => set("amount", e.target.value)}
        />
        {fieldNote("amount")}
      </label>

      <label>
        Currency
        <input value={values.currency} maxLength={3} onChange={(e) => set("currency", e.target.value)} />
        {fieldNote("currency")}
      </label>

      <label>
        Date
        <input
          type="date"
          required
          value={values.expenseDate}
          onChange={(e) => set("expenseDate", e.target.value)}
        />
        {fieldNote("expenseDate")}
      </label>

      <label>
        Category
        <select required value={values.categoryId} onChange={(e) => set("categoryId", e.target.value)}>
          <option value="">Select a category</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        {fieldNote("categoryId")}
      </label>

      <label>
        Merchant
        <input value={values.merchant} onChange={(e) => set("merchant", e.target.value)} />
        {fieldNote("merchant")}
      </label>

      <label>
        Description
        <input value={values.description} onChange={(e) => set("description", e.target.value)} />
        {fieldNote("description")}
      </label>

      {errors.analysisId && (
        <p className="error" role="alert">
          {errors.analysisId[0]}
        </p>
      )}

      {message && (
        <p className="error" role="alert">
          {message}
        </p>
      )}

      <div className="actions">
        <button type="submit" disabled={saving}>
          {saving ? "Saving..." : "Save"}
        </button>
        <button type="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
      </div>
    </form>
  );
}

export default ExpenseForm;
