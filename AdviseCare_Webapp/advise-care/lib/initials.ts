/** Two initials from the last two words of a Vietnamese or English name. */
export function getInitials(name: string): string {
  return name
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(-2)
    .map((part) => part[0]?.toLocaleUpperCase() ?? "")
    .join("");
}
