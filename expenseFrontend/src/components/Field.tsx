import type { ReactNode } from "react";
import { AlertCircleIcon } from "../icons";

type Props = {
  id: string;
  label: string;
  optional?: boolean;
  // Shown after the label, e.g. the "Suggested" / "Edited" chip on the receipt review.
  chip?: ReactNode;
  error?: string;
  helper?: ReactNode;
  // "ai" for "Not detected — enter manually" on the receipt review.
  helperTone?: "muted" | "ai";
  className?: string;
  children: ReactNode;
};

// Label, control and one message line (§C.6). The control must set
// aria-invalid={Boolean(error)} and aria-describedby={`${id}-msg`} when there is a message.
function Field({ id, label, optional, chip, error, helper, helperTone = "muted", className, children }: Props) {
  return (
    <div className={className ? `field ${className}` : "field"}>
      <label className="field__label" htmlFor={id}>
        {label}
        {optional && <span className="muted"> (optional)</span>}
        {chip}
      </label>
      {children}
      {error ? (
        <p id={`${id}-msg`} className="field__msg field__msg--error">
          <AlertCircleIcon size={14} />
          {error}
        </p>
      ) : (
        helper && (
          <p id={`${id}-msg`} className={`field__msg field__msg--${helperTone}`}>
            {helper}
          </p>
        )
      )}
    </div>
  );
}

export default Field;
