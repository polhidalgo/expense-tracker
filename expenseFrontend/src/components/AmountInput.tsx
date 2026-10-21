import type { InputHTMLAttributes } from "react";

type Props = Omit<InputHTMLAttributes<HTMLInputElement>, "type"> & {
  large?: boolean;
};

// Amount input with a non-editable "A$" prefix (§C.6). It stays type="number" step="any",
// so the value and the Number() conversion on submit are exactly as before.
function AmountInput({ large, className, ...props }: Props) {
  return (
    <div className={large ? "amount-input amount-input--lg" : "amount-input"}>
      <span className="amount-input__prefix" aria-hidden="true">
        A$
      </span>
      <input {...props} className={className} type="number" step="any" inputMode="decimal" />
    </div>
  );
}

export default AmountInput;
