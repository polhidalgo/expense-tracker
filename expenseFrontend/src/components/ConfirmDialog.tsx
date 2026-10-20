import { useEffect, useRef } from "react";

type Props = {
  message: string;
  onConfirm: () => void;
  onCancel: () => void;
};

function ConfirmDialog({ message, onConfirm, onCancel }: Props) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    if (ref.current && !ref.current.open) ref.current.showModal();
  }, []);

  return (
    <dialog ref={ref} onClose={onCancel}>
      <p>{message}</p>
      <div className="actions">
        <button type="button" onClick={onConfirm}>
          Delete
        </button>
        <button type="button" onClick={onCancel} autoFocus>
          Cancel
        </button>
      </div>
    </dialog>
  );
}

export default ConfirmDialog;
