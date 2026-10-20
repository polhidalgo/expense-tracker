import type { Category } from "../types";

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

function ExpenseFilters({ filters, categories, onChange }: Props) {
  return (
    <div className="filters">
      <label>
        From
        <input
          type="date"
          value={filters.startDate}
          onChange={(e) => onChange({ ...filters, startDate: e.target.value })}
        />
      </label>
      <label>
        To
        <input
          type="date"
          value={filters.endDate}
          onChange={(e) => onChange({ ...filters, endDate: e.target.value })}
        />
      </label>
      <label>
        Category
        <select
          value={filters.categoryId}
          onChange={(e) => onChange({ ...filters, categoryId: e.target.value })}
        >
          <option value="">All</option>
          {categories.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>
      </label>
      <button
        type="button"
        onClick={() => onChange({ startDate: "", endDate: "", categoryId: "" })}
      >
        Clear
      </button>
    </div>
  );
}

export default ExpenseFilters;
