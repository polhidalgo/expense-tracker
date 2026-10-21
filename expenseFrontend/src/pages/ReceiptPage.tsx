import { useEffect, useMemo, useRef, useState, type DragEvent, type FormEvent } from "react";
import { Link } from "react-router-dom";
import { ApiError, analyzeReceipt, createExpense, errorMessage, getCategories } from "../api";
import Alert from "../components/Alert";
import ExpenseForm, { type ExpenseFormValues } from "../components/ExpenseForm";
import { formatCurrency, formatDate } from "../format";
import { SparkleIcon, UploadIcon } from "../icons";
import type { Category, ExpenseRequest, ReceiptDraft } from "../types";
import { usePageTitle } from "../usePageTitle";

type AnalyzeError = { message: string; canRetry: boolean };

// Same limits as the server, which stays the authority; this only avoids a pointless upload.
const ACCEPTED_TYPES = ["image/jpeg", "image/png", "image/webp"];
const MAX_FILE_BYTES = 3 * 1024 * 1024;
const SLOW_AFTER_MS = 8000;

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

// The fields the AI did fill in. Description is never AI-filled.
function suggested(draft: ReceiptDraft): (keyof ExpenseFormValues)[] {
  const fields: (keyof ExpenseFormValues)[] = [];
  if (draft.amount !== null) fields.push("amount");
  if (draft.expenseDate !== null) fields.push("expenseDate");
  if (draft.categoryId !== null) fields.push("categoryId");
  if (draft.merchant !== null) fields.push("merchant");
  return fields;
}

function fileProblem(file: File): string {
  if (!ACCEPTED_TYPES.includes(file.type)) return "Choose a JPEG, PNG or WebP image.";
  if (file.size > MAX_FILE_BYTES) return "The file is larger than 3 MB.";
  return "";
}

function formatSize(bytes: number): string {
  return bytes < 1024 * 1024 ? `${Math.round(bytes / 1024)} KB` : `${(bytes / 1024 / 1024).toFixed(1)} MB`;
}

const STEPS = ["Upload", "Review", "Saved"];

function ReceiptPage() {
  usePageTitle("Scan receipt");
  const [categories, setCategories] = useState<Category[]>([]);
  const [file, setFile] = useState<File | null>(null);
  const [analyzing, setAnalyzing] = useState(false);
  const [slow, setSlow] = useState(false);
  const [error, setError] = useState<AnalyzeError | null>(null);
  const [draft, setDraft] = useState<ReceiptDraft | null>(null);
  const [saved, setSaved] = useState<ExpenseRequest | null>(null);
  const [dragOver, setDragOver] = useState(false);
  const [imageExpanded, setImageExpanded] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  // The values just submitted, for the "Expense saved" summary (no extra API call).
  const submitted = useRef<ExpenseRequest | null>(null);

  const previewUrl = useMemo(() => (file ? URL.createObjectURL(file) : null), [file]);
  useEffect(() => () => {
    if (previewUrl) URL.revokeObjectURL(previewUrl);
  }, [previewUrl]);

  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch(() => setCategories([]));
  }, []);

  const problem = file ? fileProblem(file) : "";

  function chooseFile(next: File | null) {
    setFile(next);
    setError(null);
    setSaved(null);
  }

  async function analyze(e?: FormEvent) {
    e?.preventDefault();
    if (!file || problem) return;
    setAnalyzing(true);
    setSlow(false);
    setError(null);
    setSaved(null);
    const slowTimer = setTimeout(() => setSlow(true), SLOW_AFTER_MS);
    try {
      setDraft(await analyzeReceipt(file));
    } catch (err) {
      setError(errorFrom(err));
    } finally {
      clearTimeout(slowTimer);
      setAnalyzing(false);
    }
  }

  function reset() {
    setDraft(null);
    setFile(null);
    setImageExpanded(false);
  }

  function handleDrop(e: DragEvent) {
    e.preventDefault();
    setDragOver(false);
    const dropped = e.dataTransfer.files?.[0];
    if (dropped) chooseFile(dropped);
  }

  const step = saved ? 2 : draft ? 1 : 0;

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Scan receipt</h1>
          <ol className="stepper" aria-label="Steps">
            {STEPS.map((label, i) => (
              <li key={label} className={i === step ? "is-current" : undefined} aria-current={i === step ? "step" : undefined}>
                {i + 1} {label}
              </li>
            ))}
          </ol>
        </div>
      </div>

      {/* Stage 3: saved */}
      {saved && (
        <div className="saved-block" role="status">
          <h2>Expense saved</h2>
          <p className="muted">
            {[saved.merchant, formatCurrency(saved.amount, saved.currency ?? "AUD"), formatDate(saved.expenseDate)]
              .filter(Boolean)
              .join(" · ")}
          </p>
          <div className="actions">
            <button className="btn btn--primary" type="button" onClick={() => setSaved(null)}>
              Scan another
            </button>
            <Link className="btn btn--secondary" to="/expenses">
              View in expenses
            </Link>
          </div>
        </div>
      )}

      {/* Stage 1: upload */}
      {!saved && !draft && (
        <form className="stack" onSubmit={analyze}>
          <input
            ref={inputRef}
            id="receipt-file"
            className="sr-only"
            type="file"
            accept={ACCEPTED_TYPES.join(",")}
            onChange={(e) => chooseFile(e.target.files?.[0] ?? null)}
          />

          {!file ? (
            <div
              className={dragOver ? "dropzone is-over" : "dropzone"}
              onDragOver={(e) => {
                e.preventDefault();
                setDragOver(true);
              }}
              onDragLeave={() => setDragOver(false)}
              onDrop={handleDrop}
            >
              <span className="muted">
                <UploadIcon size={20} />
              </span>
              <p className="dropzone__title">Drop a receipt or payment screenshot</p>
              <p>
                or{" "}
                <label className="link" htmlFor="receipt-file">
                  choose a file
                </label>
              </p>
              <p className="muted small">JPEG, PNG or WebP · up to 3 MB</p>
            </div>
          ) : (
            <div className="file-row">
              <div className="file-row__thumb">
                {previewUrl && <img src={previewUrl} alt="" />}
                {analyzing && <span className="file-row__overlay">Reading receipt…</span>}
              </div>
              <div className="file-row__info">
                <span className="mono small">{file.name}</span>
                <span className="muted small">{formatSize(file.size)}</span>
                {problem && <span className="error small">{problem}</span>}
              </div>
              <div className="actions">
                <button
                  className="btn btn--ghost"
                  type="button"
                  disabled={analyzing}
                  onClick={() => inputRef.current?.click()}
                >
                  Replace
                </button>
                <button className="btn btn--primary" type="submit" disabled={analyzing || Boolean(problem)}>
                  {analyzing ? "Analysing…" : "Analyse receipt"}
                </button>
              </div>
            </div>
          )}

          {analyzing && slow && <p className="muted">This can take a few seconds.</p>}

          {error && (
            <Alert
              action={
                error.canRetry ? (
                  <button className="btn btn--secondary btn--sm" type="button" onClick={() => analyze()} disabled={analyzing}>
                    Retry
                  </button>
                ) : (
                  <button className="btn btn--secondary btn--sm" type="button" onClick={() => inputRef.current?.click()}>
                    Choose another file
                  </button>
                )
              }
            >
              {error.message}
            </Alert>
          )}

          <p className="muted">Nothing is saved until you review and confirm.</p>
        </form>
      )}

      {/* Stage 2: review next to the image */}
      {!saved && draft && (
        <div className="review-grid">
          <div className="review-grid__image">
            {previewUrl && (
              <button
                className={imageExpanded ? "receipt-image is-expanded" : "receipt-image"}
                type="button"
                aria-label={imageExpanded ? "Collapse receipt image" : "Expand receipt image"}
                aria-expanded={imageExpanded}
                onClick={() => setImageExpanded(!imageExpanded)}
              >
                <img src={previewUrl} alt="Uploaded receipt" />
              </button>
            )}
          </div>
          <div className="stack">
            <p className="notice notice--ai" role="note">
              <SparkleIcon />
              AI suggestions may be wrong. Review before saving.
            </p>
            <ExpenseForm
              title="Review expense"
              submitLabel="Save expense"
              cancelLabel="Discard"
              categories={categories}
              initial={toFormValues(draft)}
              notDetected={notDetected(draft)}
              suggested={suggested(draft)}
              onSubmit={(body) => {
                submitted.current = body;
                return createExpense(body);
              }}
              onSaved={() => {
                reset();
                setSaved(submitted.current);
              }}
              onCancel={reset}
            />
          </div>
        </div>
      )}
    </>
  );
}

export default ReceiptPage;
