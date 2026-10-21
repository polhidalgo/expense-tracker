import { useEffect, useId, useRef } from "react";

type Props = {
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
  // Optional, with defaults that keep the old behaviour (§C.10).
  title?: string;
  confirmLabel?: string;
};

function ConfirmDialog({ message, onConfirm, onCancel, title, confirmLabel = "Delete" }: Props) {
  const ref = useRef<HTMLDialogElement>(null);
  const id = useId();

  useEffect(() => {
    const opener = document.activeElement as HTMLElement | null;
    if (ref.current && !ref.current.open) ref.current.showModal();
    return () => opener?.focus();
  }, []);

  return (
    <dialog
      ref={ref}
      className="dialog"
      aria-labelledby={title ? `${id}-title` : undefined}
      aria-describedby={`${id}-message`}
      onClose={onCancel}
      onClick={(e) => e.target === ref.current && onCancel()}
    >
      {/* The padding lives on this wrapper, so only a click on the backdrop itself cancels. */}
      <div className="dialog__body">
        {title && <h2 id={`${id}-title`}>{title}</h2>}
        <p id={`${id}-message`}>{message}</p>
        <div className="actions">
          <button className="btn btn--secondary" type="button" onClick={onCancel} autoFocus>
            Cancel
          </button>
          <button className="btn btn--danger" type="button" onClick={onConfirm}>
            {confirmLabel}
          </button>
        </div>
      </div>
    </dialog>
  );
}

export default ConfirmDialog;
