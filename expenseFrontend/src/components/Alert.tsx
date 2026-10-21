import type { ReactNode } from "react";
import { AlertCircleIcon } from "../icons";

type Props = {
  children: ReactNode;
  // Optional action on the right, e.g. a Retry button.
  action?: ReactNode;
  // "info" for non-errors such as "Your session has expired".
  tone?: "danger" | "info";
};

// Inline message shown directly above the region it is about (§C.11).
function Alert({ children, action, tone = "danger" }: Props) {
  return (
    <div className={`alert alert--${tone}`} role="alert">
      <span className="alert__icon">
        <AlertCircleIcon />
      </span>
      <span className="alert__text">{children}</span>
      {action}
    </div>
  );
}

export default Alert;
