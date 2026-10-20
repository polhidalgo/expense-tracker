export function formatCurrency(amount: number, currency: string): string {
  return new Intl.NumberFormat("en-AU", { style: "currency", currency }).format(amount);
}

export function toIsoDate(date: Date): string {
  return date.toLocaleDateString("en-CA");
}
