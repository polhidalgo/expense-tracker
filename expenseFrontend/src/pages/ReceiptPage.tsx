import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { ApiError, analyzeReceipt, createExpense, errorMessage, getCategories } from "../api";
import ExpenseForm, { type ExpenseFormValues } from "../components/ExpenseForm";
import type { Category, ReceiptDraft } from "../types";

type AnalyzeError = { message: string; canRetry: boolean };

// Retry only helps when Groq is busy (429) or unavailable (503).
function errorFrom(error: unknown): AnalyzeError {
  const canRetry = error instanceof ApiError && (error.status === 429 || error.status === 503);
  return { message: errorMessage(error), canRetry };
}

// Empty AI values stay empty, so the user has to fill them in. Only the currency falls back to AUD,
// the only supported currency.
function toFormValues(draft: ReceiptDraft): ExpenseFormValues {
  return {
    amount: draft.amount?.toString() ?? "",
    currency: draft.currency ?? "AUD",
    expenseDate: draft.expenseDate ?? "",
    categoryId: draft.categoryId ?? "",
    merchant: draft.merchant ?? "",
    description: "",
  };
}

function notDetected(draft: ReceiptDraft): (keyof ExpenseFormValues)[] {
  const fields: (keyof ExpenseFormValues)[] = [];
  if (draft.amount === null) fields.push("amount");
  if (draft.expenseDate === null) fields.push("expenseDate");
  if (draft.categoryId === null) fields.push("categoryId");
  if (draft.merchant === null) fields.push("merchant");
  return fields;
}

function ReceiptPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [file, setFile] = useState<File | null>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [error, setError] = useState<AnalyzeError | null>(null);
  const [draft, setDraft] = useState<ReceiptDraft | null>(null);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch(() => setCategories([]));
  }, []);

  async function analyze(e?: FormEvent) {
    e?.preventDefault();
    if (!file) return;
    setAnalyzing(true);
    setError(null);
    setSaved(false);
    try {
      setDraft(await analyzeReceipt(file));
    } catch (err) {
      setError(errorFrom(err));
    } finally {
      setAnalyzing(false);
    }
  }

  function reset() {
    setDraft(null);
    setFile(null);
  }

  return (
    <>
      <h1>Receipt</h1>

      {!draft && (
        <form className="filters" onSubmit={analyze}>
          <label>
            Receipt or payment screenshot (JPEG, PNG or WebP, max 3 MB)
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp"
              required
              onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            />
          </label>
          <button type="submit" disabled={!file || analyzing}>
            {analyzing ? "Analyzing..." : "Analyze"}
          </button>
        </form>
      )}

      {error && (
        <div className="actions">
          <p className="error" role="alert">
            {error.message}
          </p>
          {error.canRetry && (
            <button type="button" onClick={() => analyze()} disabled={analyzing}>
              Retry
            </button>
          )}
        </div>
      )}

      {saved && (
        <p role="status">
          Expense saved. <Link to="/expenses">See expenses</Link>
        </p>
      )}

      {draft && (
        <>
          <p className="notice" role="note">
            AI suggestions may be wrong. Review before saving.
          </p>
          <ExpenseForm
            title="Review expense"
            categories={categories}
            initial={toFormValues(draft)}
            notDetected={notDetected(draft)}
            onSubmit={createExpense}
            onSaved={() => {
              reset();
              setSaved(true);
            }}
            onCancel={reset}
          />
        </>
      )}
    </>
  );
}

export default ReceiptPage;
