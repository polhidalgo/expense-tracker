import { useRef, useState, type FormEvent } from "react";
import { ApiError, createExpense, updateExpense } from "../api";
import { toIsoDate } from "../format";
import type { Category, Expense, FieldErrors } from "../types";

type Props = {
  expense?: Expense;
  categories: Category[];
  onSaved: () => void;
  onCancel: () => void;
};

function ExpenseForm({ expense, categories, onSaved, onCancel }: Props) {
  const [amount, setAmount] = useState(expense ? String(expense.amount) : "");
  const [currency, setCurrency] = useState(expense?.currency ?? "AUD");
  const [expenseDate, setExpenseDate] = useState(expense?.expenseDate ?? toIsoDate(new Date()));
  const [categoryId, setCategoryId] = useState(expense?.categoryId ?? "");
  const [merchant, setMerchant] = useState(expense?.merchant ?? "");
  const [description, setDescription] = useState(expense?.description ?? "");
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (savingRef.current) return;
    savingRef.current = true;
    setSaving(true);
    setErrors({});
    setMessage("");

    const body = {
      amount: Number(amount),
      currency,
      expenseDate,
      categoryId,
      merchant: merchant || undefined,
      description: description || undefined,
    };

    try {
      if (expense) await updateExpense(expense.id, body);
      else await createExpense(body);
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

  const fieldError = (name: string) => {
    const text = errors[name]?.[0] ?? errors[`$.${name}`]?.[0];
    return text && <span className="error">{text}</span>;
  };

  return (
    <form className="expense-form" onSubmit={handleSubmit}>
      <h2>{expense ? "Edit expense" : "New expense"}</h2>

      <label>
        Amount
        <input
          type="number"
          step="any"
          required
          value={amount}
          onChange={(e) => setAmount(e.target.value)}
        />
        {fieldError("amount")}
      </label>

      <label>
        Currency
        <input value={currency} maxLength={3} onChange={(e) => setCurrency(e.target.value)} />
        {fieldError("currency")}
      </label>

      <label>
        Date
        <input
          type="date"
          required
          value={expenseDate}
          onChange={(e) => setExpenseDate(e.target.value)}
        />
        {fieldError("expenseDate")}
      </label>

      <label>
        Category
        <select required value={categoryId} onChange={(e) => setCategoryId(e.target.value)}>
          <option value="">Select a category</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
        {fieldError("categoryId")}
      </label>

      <label>
        Merchant
        <input value={merchant} onChange={(e) => setMerchant(e.target.value)} />
        {fieldError("merchant")}
      </label>

      <label>
        Description
        <input value={description} onChange={(e) => setDescription(e.target.value)} />
        {fieldError("description")}
      </label>

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
