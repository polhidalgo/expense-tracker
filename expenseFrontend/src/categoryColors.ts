// One fixed colour per category id (FR-CAT-002), so colours never shift.
// The values are CSS custom properties from styles/tokens.css, so they follow the theme.
const CATEGORY_COLORS: Record<string, string> = {
  "00000000-0000-0000-0000-000000000101": "var(--cat-groceries)",
  "00000000-0000-0000-0000-000000000102": "var(--cat-dining)",
  "00000000-0000-0000-0000-000000000103": "var(--cat-transport)",
  "00000000-0000-0000-0000-000000000104": "var(--cat-housing)",
  "00000000-0000-0000-0000-000000000105": "var(--cat-bills)",
  "00000000-0000-0000-0000-000000000106": "var(--cat-health)",
  "00000000-0000-0000-0000-000000000107": "var(--cat-shopping)",
  "00000000-0000-0000-0000-000000000108": "var(--cat-entertainment)",
  "00000000-0000-0000-0000-000000000109": "var(--cat-travel)",
  "00000000-0000-0000-0000-000000000110": "var(--cat-other)",
};

// An unknown id gets the "Other" colour.
export function categoryColor(categoryId: string): string {
  return CATEGORY_COLORS[categoryId] ?? "var(--cat-other)";
}
