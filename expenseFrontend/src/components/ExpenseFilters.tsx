import { useState } from "react";
import type { Category } from "../types";
import CategorySelect from "./CategorySelect";
import Sheet from "./Sheet";

export type Filters = {
  startDate: string;
  endDate: string;
  categoryId: string;
};

type Props = {
  filters: Filters;
  categories: Category[];
  onChange: (filters: Filters) => void;
};

const noFilters: Filters = { startDate: "", endDate: "", categoryId: "" };

// Compact filter bar (§C.12): labels are visually hidden, active filters use accent styling,
// and "Clear filters" only shows when something is set. On small screens the same controls
// open in a bottom sheet from a "Filters" button (T17).
function ExpenseFilters({ filters, categories, onChange }: Props) {
  const [sheetOpen, setSheetOpen] = useState(false);
  const active = (value: string) => (value ? "is-active" : undefined);
  const setCount = [filters.startDate, filters.endDate, filters.categoryId].filter(Boolean).length;

  // The controls, with an id prefix so the bar and the sheet do not clash.
  const controls = (prefix: string, stacked: boolean) => {
    const label = (htmlFor: string, text: string) => (
      <label className={stacked ? "field__label" : "sr-only"} htmlFor={`${prefix}-${htmlFor}`}>
        {text}
      </label>
    );
    return (
      <>
        <div className={stacked ? "field" : "filter-bar__item"}>
          {label("from", "From")}
          <input
            id={`${prefix}-from`}
            className={active(filters.startDate)}
            type="date"
            value={filters.startDate}
            onChange={(e) => onChange({ ...filters, startDate: e.target.value })}
          />
        </div>
        {!stacked && (
          <span className="muted" aria-hidden="true">
            –
          </span>
        )}
        <div className={stacked ? "field" : "filter-bar__item"}>
          {label("to", "To")}
          <input
            id={`${prefix}-to`}
            className={active(filters.endDate)}
            type="date"
            value={filters.endDate}
            onChange={(e) => onChange({ ...filters, endDate: e.target.value })}
          />
        </div>
        <div className={stacked ? "field" : "filter-bar__item"}>
          {label("category", "Category")}
          <CategorySelect
            id={`${prefix}-category`}
            className={active(filters.categoryId)}
            value={filters.categoryId}
            categories={categories}
            emptyLabel="All categories"
            emptySelectable
            onChange={(categoryId) => onChange({ ...filters, categoryId })}
          />
        </div>
      </>
    );
  };

  return (
    <>
      <div className="filter-bar" role="group" aria-label="Filters">
        {controls("filter", false)}
        {setCount > 0 && (
          <button className="btn btn--ghost" type="button" onClick={() => onChange(noFilters)}>
            Clear filters
          </button>
        )}
      </div>

      <button
        className={setCount > 0 ? "btn btn--secondary filter-toggle is-active" : "btn btn--secondary filter-toggle"}
        type="button"
        onClick={() => setSheetOpen(true)}
      >
        {setCount > 0 ? `Filters · ${setCount}` : "Filters"}
      </button>

      {sheetOpen && (
        <Sheet label="Filters" variant="bottom" onClose={() => setSheetOpen(false)}>
          {(close) => (
            <div className="form">
              <h2 className="form__title">Filters</h2>
              <div className="form__body">{controls("filter-sheet", true)}</div>
              <div className="form__footer">
                {setCount > 0 && (
                  <button className="btn btn--ghost" type="button" onClick={() => onChange(noFilters)}>
                    Clear filters
                  </button>
                )}
                <button className="btn btn--primary" type="button" onClick={close}>
                  Done
                </button>
              </div>
            </div>
          )}
        </Sheet>
      )}
    </>
  );
}

export default ExpenseFilters;
