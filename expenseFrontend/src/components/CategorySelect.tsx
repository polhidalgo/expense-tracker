import type { SelectHTMLAttributes } from "react";
import { categoryColor } from "../categoryColors";
import type { Category } from "../types";

type Props = Omit<SelectHTMLAttributes<HTMLSelectElement>, "onChange" | "value"> & {
  value: string;
  categories: Category[];
  onChange: (categoryId: string) => void;
  // "Select a category" in forms (cannot be chosen again), "All categories" in filters.
  emptyLabel: string;
  emptySelectable?: boolean;
};

// Native select with the chosen category's colour square (§C.7). Options in API order.
function CategorySelect({ value, categories, onChange, emptyLabel, emptySelectable = false, className, ...props }: Props) {
  return (
    <div className={value ? "category-select category-select--chosen" : "category-select"}>
      {value && <span className="swatch category-select__swatch" style={{ background: categoryColor(value) }} />}
      <select {...props} className={className} value={value} onChange={(e) => onChange(e.target.value)}>
        <option value="" disabled={!emptySelectable && value !== ""} hidden={!emptySelectable && value !== ""}>
          {emptyLabel}
        </option>
        {categories.map((c) => (
          <option key={c.id} value={c.id}>
            {c.name}
          </option>
        ))}
      </select>
    </div>
  );
}

export default CategorySelect;
