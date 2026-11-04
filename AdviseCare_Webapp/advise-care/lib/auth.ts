import { redirect } from "next/navigation";
import { ApiError } from "./api-contract";
import { apiGet } from "./api";

export interface AuthenticatedUser {
  accountId: string;
  facultyId: string | null;
  avatarUrl: string;
  email: string;
  fullName: string;
  authProvider: string;
  studentCode: string;
  userName: string;
  createdAt: string;
  hasPassword: boolean;
  academicStatus: string;
  linkStatus: string;
  preferredLanguage: "en" | "vi";
  status: string;
  roles: string[];
}

export async function getCurrentUser(): Promise<AuthenticatedUser> {
  return apiGet<AuthenticatedUser>("Auth/me");
}

export async function requireUser(
  locale: string,
): Promise<AuthenticatedUser> {
  try {
    return await getCurrentUser();
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      redirect(`/${locale}/sign-in?reason=expired`);
    }
    if (error instanceof ApiError && error.status === 403) {
      redirect(`/${locale}/sign-in?reason=account`);
    }
    throw error;
  }
}

export async function requireRole(
  locale: string,
  roles: readonly string[],
): Promise<AuthenticatedUser> {
  const user = await requireUser(locale);
  if (!roles.some((role) => user.roles.includes(role))) {
    redirect(`/${locale}/access-denied`);
  }
  return user;
}

export { getInitials } from "./initials";
