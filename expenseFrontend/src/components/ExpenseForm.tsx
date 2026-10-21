import { useId, useRef, useState, type FormEvent } from "react";
import { ApiError } from "../api";
import { toIsoDate } from "../format";
import type { Category, ExpenseRequest, FieldErrors } from "../types";
import Alert from "./Alert";
import AmountInput from "./AmountInput";
import CategorySelect from "./CategorySelect";
import Field from "./Field";

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
  // Fields the AI filled in: marked "Suggested" until the user edits them (receipt review).
  suggested?: (keyof ExpenseFormValues)[];
  submitLabel?: string;
  cancelLabel?: string;
  onSubmit: (body: ExpenseRequest) => Promise<unknown>;
  onSaved: () => void;
  onCancel: () => void;
};

const DESCRIPTION_MAX = 500;
const DESCRIPTION_COUNTER_FROM = 400;

const emptyValues = (): ExpenseFormValues => ({
  amount: "",
  currency: "AUD",
  expenseDate: toIsoDate(new Date()),
  categoryId: "",
  merchant: "",
  description: "",
});

function ExpenseForm({
  title,
  categories,
  initial,
  notDetected = [],
  suggested = [],
  submitLabel = "Save",
  cancelLabel = "Cancel",
  onSubmit,
  onSaved,
  onCancel,
}: Props) {
  const [values, setValues] = useState<ExpenseFormValues>(initial ?? emptyValues);
  const [touched, setTouched] = useState<(keyof ExpenseFormValues)[]>([]);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const savingRef = useRef(false);
  const id = useId();

  const set = (field: keyof ExpenseFormValues, value: string) => {
    setValues({ ...values, [field]: value });
    if (!touched.includes(field)) setTouched([...touched, field]);
  };

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

  const errorOf = (name: keyof ExpenseFormValues) => errors[name]?.[0] ?? errors[`$.${name}`]?.[0];

  // "Suggested" until edited, then "Edited" (§C.8). Not-detected fields get a helper instead.
  const isSuggested = (name: keyof ExpenseFormValues) => suggested.includes(name) && !touched.includes(name);
  const chip = (name: keyof ExpenseFormValues) =>
    suggested.includes(name) &&
    (touched.includes(name) ? (
      <span className="badge badge--muted">Edited</span>
    ) : (
      <span className="badge badge--ai">Suggested</span>
    ));
  const helper = (name: keyof ExpenseFormValues) =>
    notDetected.includes(name) && !touched.includes(name) ? "Not detected — enter manually" : undefined;

  // Props shared by every control: id, invalid state, the message it is described by and its classes.
  // extraHelper is a field's own helper (counter, "Only AUD"), shown when there is no other message.
  const control = (name: keyof ExpenseFormValues, extraHelper?: string, extraClass?: string) => {
    const hasMessage = Boolean(errorOf(name) || helper(name) || extraHelper);
    const classes = [isSuggested(name) ? "is-suggested" : "", extraClass ?? ""].filter(Boolean).join(" ");
    return {
      id: `${id}-${name}`,
      "aria-invalid": Boolean(errorOf(name)),
      "aria-describedby": hasMessage ? `${id}-${name}-msg` : undefined,
      className: classes || undefined,
    };
  };

  // "Not detected" wins over the field's own helper.
  const fieldProps = (name: keyof ExpenseFormValues, extraHelper?: string) => ({
    id: `${id}-${name}`,
    chip: chip(name),
    error: errorOf(name),
    helper: helper(name) ?? extraHelper,
    helperTone: helper(name) ? ("ai" as const) : ("muted" as const),
  });

  const descriptionCounter =
    values.description.length > DESCRIPTION_COUNTER_FROM ? `${values.description.length} / ${DESCRIPTION_MAX}` : undefined;

  // Only AUD is accepted: read-only unless an AI draft brought another code to correct.
  const currencyReadOnly = values.currency === "AUD";
  const currencyHelper = currencyReadOnly ? "Only AUD is supported." : undefined;

  return (
    <form className="form" onSubmit={handleSubmit}>
      <h2 className="form__title">{title}</h2>

      <div className="form__body">
        {message && <Alert>{message}</Alert>}

        <Field label="Amount" {...fieldProps("amount")}>
          <AmountInput
            {...control("amount")}
            large
            required
            autoFocus={!initial}
            value={values.amount}
            onChange={(e) => set("amount", e.target.value)}
          />
        </Field>

        <div className="form__row">
          <Field label="Date" {...fieldProps("expenseDate")}>
            <input
              {...control("expenseDate")}
              type="date"
              required
              value={values.expenseDate}
              onChange={(e) => set("expenseDate", e.target.value)}
            />
          </Field>

          <Field label="Category" {...fieldProps("categoryId")}>
            <CategorySelect
              {...control("categoryId")}
              required
              value={values.categoryId}
              categories={categories}
              emptyLabel="Select a category"
              onChange={(value) => set("categoryId", value)}
            />
          </Field>
        </div>

        <Field label="Merchant" optional {...fieldProps("merchant")}>
          <input {...control("merchant")} value={values.merchant} onChange={(e) => set("merchant", e.target.value)} />
        </Field>

        <Field label="Description" optional {...fieldProps("description", descriptionCounter)}>
          <textarea
            {...control("description", descriptionCounter)}
            rows={3}
            maxLength={DESCRIPTION_MAX}
            value={values.description}
            onChange={(e) => set("description", e.target.value)}
          />
        </Field>

        <Field label="Currency" {...fieldProps("currency", currencyHelper)}>
          <input
            {...control("currency", currencyHelper, "mono")}
            maxLength={3}
            readOnly={currencyReadOnly}
            value={values.currency}
            onChange={(e) => set("currency", e.target.value)}
          />
        </Field>
      </div>

      <div className="form__footer">
        <button className="btn btn--secondary" type="button" onClick={onCancel} disabled={saving}>
          {cancelLabel}
        </button>
        <button className="btn btn--primary" type="submit" disabled={saving}>
          {saving ? "Saving…" : submitLabel}
        </button>
      </div>
    </form>
  );
}

export default ExpenseForm;
