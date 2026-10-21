import { useEffect, useRef, useState, type ReactNode } from "react";
import { XIcon } from "../icons";
import ConfirmDialog from "./ConfirmDialog";

type Props = {
  label: string;
  onClose: () => void;
  // "side" for create / edit forms (§C.6), "bottom" for the mobile filter sheet (§C.12).
  variant?: "side" | "bottom";
  // Ask "Discard changes?" before closing once something was typed (T20).
  guardChanges?: boolean;
  // Receives the guarded close, to use as the form's Cancel.
  children: (requestClose: () => void) => ReactNode;
};

// A native modal <dialog>, full screen on small screens. Escape, the close button and a click on the
// backdrop all close it; focus returns to where it was.
function Sheet({ label, onClose, variant = "side", guardChanges = false, children }: Props) {
  const ref = useRef<HTMLDialogElement>(null);
  // Any input or change inside the sheet makes it dirty; the forms themselves are untouched.
  const [dirty, setDirty] = useState(false);
  const [confirming, setConfirming] = useState(false);

  useEffect(() => {
    const opener = document.activeElement as HTMLElement | null;
    if (ref.current && !ref.current.open) ref.current.showModal();
    return () => opener?.focus();
  }, []);

  function requestClose() {
    if (guardChanges && dirty) setConfirming(true);
    else onClose();
  }

  return (
    <>
      <dialog
        ref={ref}
        className={`sheet sheet--${variant}`}
        aria-label={label}
        onInput={() => !dirty && setDirty(true)}
        onCancel={(e) => {
          // Escape: keep the dialog open and ask first when there are changes.
          e.preventDefault();
          requestClose();
        }}
        onClose={onClose}
        onClick={(e) => e.target === ref.current && requestClose()}
      >
        <button className="btn btn--ghost btn--icon sheet__close" type="button" aria-label="Close" onClick={requestClose}>
          <XIcon />
        </button>
        {children(requestClose)}
      </dialog>

      {confirming && (
        <ConfirmDialog
          title="Discard changes?"
          message="Your changes to this form will be lost."
          confirmLabel="Discard"
          onConfirm={() => {
            setConfirming(false);
            onClose();
          }}
          onCancel={() => setConfirming(false)}
        />
      )}
    </>
  );
}

export default Sheet;
