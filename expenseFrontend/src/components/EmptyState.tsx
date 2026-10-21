import type { ReactNode } from "react";

type Props = {
  title: string;
  description?: ReactNode;
  // One action (or a small group), e.g. "Add expense".
  actions?: ReactNode;
};

// Text-only empty state, no illustration (§C.11).
function EmptyState({ title, description, actions }: Props) {
  return (
    <div className="empty">
      <p className="empty__title">{title}</p>
      {description && <p className="muted">{description}</p>}
      {actions && <div className="actions">{actions}</div>}
    </div>
  );
}

export default EmptyState;
