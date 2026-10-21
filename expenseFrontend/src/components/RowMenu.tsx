import { useEffect, useRef, useState, type KeyboardEvent } from "react";
import { MoreHorizontalIcon } from "../icons";

type Props = {
  // Names the row, e.g. "Woolworths", for the button's accessible name.
  label: string;
  onEdit: () => void;
  onDelete: () => void;
};

// One "⋯" button per row with Edit / Delete (§C.5, T18).
// Enter / Space open it, arrow keys move, Escape closes and returns focus to the button.
function RowMenu({ label, onEdit, onDelete }: Props) {
  const [open, setOpen] = useState(false);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    menuRef.current?.querySelector<HTMLButtonElement>("[role=menuitem]")?.focus();

    // A click anywhere else closes the menu.
    function onPointerDown(e: PointerEvent) {
      if (!menuRef.current?.contains(e.target as Node) && !buttonRef.current?.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("pointerdown", onPointerDown);
    return () => document.removeEventListener("pointerdown", onPointerDown);
  }, [open]);

  function close() {
    setOpen(false);
    buttonRef.current?.focus();
  }

  // Focus goes back to the row's button first, so a sheet or dialog opened next returns there.
  function choose(action: () => void) {
    close();
    action();
  }

  function onMenuKeyDown(e: KeyboardEvent) {
    const items = Array.from(menuRef.current?.querySelectorAll<HTMLButtonElement>("[role=menuitem]") ?? []);
    const index = items.indexOf(document.activeElement as HTMLButtonElement);
    if (e.key === "ArrowDown" || e.key === "ArrowUp") {
      e.preventDefault();
      const step = e.key === "ArrowDown" ? 1 : -1;
      items[(index + step + items.length) % items.length]?.focus();
    } else if (e.key === "Home" || e.key === "End") {
      e.preventDefault();
      items[e.key === "Home" ? 0 : items.length - 1]?.focus();
    } else if (e.key === "Escape") {
      e.preventDefault();
      close();
    } else if (e.key === "Tab") {
      setOpen(false);
    }
  }

  return (
    <div className="row-menu">
      <button
        ref={buttonRef}
        className="btn btn--ghost btn--icon row-menu__button"
        type="button"
        aria-label={`Actions for ${label}`}
        aria-haspopup="menu"
        aria-expanded={open}
        onClick={() => setOpen(!open)}
      >
        <MoreHorizontalIcon />
      </button>
      {open && (
        <div ref={menuRef} className="row-menu__list" role="menu" aria-label={`Actions for ${label}`} onKeyDown={onMenuKeyDown}>
          <button className="row-menu__item" type="button" role="menuitem" onClick={() => choose(onEdit)}>
            Edit
          </button>
          <button className="row-menu__item row-menu__item--danger" type="button" role="menuitem" onClick={() => choose(onDelete)}>
            Delete
          </button>
        </div>
      )}
    </div>
  );
}

export default RowMenu;
