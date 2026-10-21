export function formatCurrency(amount: number, currency: string): string {
  return new Intl.NumberFormat("en-AU", { style: "currency", currency }).format(amount);
}

export function toIsoDate(date: Date): string {
  return date.toLocaleDateString("en-CA");
}

// A yyyy-MM-dd value is a calendar date: read it in local time, never as UTC midnight,
// so it cannot move a day in UTC+10/+11.
function fromIsoDate(isoDate: string): Date {
  const [year, month, day] = isoDate.split("-").map(Number);
  return new Date(year, month - 1, day);
}

// Adds days to a yyyy-MM-dd date, in local time.
export function addDays(isoDate: string, days: number): string {
  const date = fromIsoDate(isoDate);
  date.setDate(date.getDate() + days);
  return toIsoDate(date);
}

// The Monday of the Monday–Sunday week that contains the date.
export function mondayOf(isoDate: string): string {
  const daysSinceMonday = (fromIsoDate(isoDate).getDay() + 6) % 7;
  return addDays(isoDate, -daysSinceMonday);
}

// "Mon 13 Oct", for the short lists on This week.
export function formatWeekdayDate(isoDate: string): string {
  return new Intl.DateTimeFormat("en-AU", { weekday: "short", day: "numeric", month: "short" }).format(
    fromIsoDate(isoDate),
  );
}

// "18 Oct" in the current year, "18 Oct 2025" otherwise.
export function formatDate(isoDate: string): string {
  const date = fromIsoDate(isoDate);
  const sameYear = date.getFullYear() === new Date().getFullYear();
  return new Intl.DateTimeFormat("en-AU", {
    day: "numeric",
    month: "short",
    year: sameYear ? undefined : "numeric",
  }).format(date);
}

// "13–19 Oct 2026", "28 Sept – 4 Oct 2026" or "29 Dec 2025 – 4 Jan 2026".
export function formatDateRange(startIsoDate: string, endIsoDate: string): string {
  return new Intl.DateTimeFormat("en-AU", { day: "numeric", month: "short", year: "numeric" }).formatRange(
    fromIsoDate(startIsoDate),
    fromIsoDate(endIsoDate),
  );
}
