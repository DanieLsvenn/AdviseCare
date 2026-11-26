import { NotificationList } from "@/components/layout/notification-list";
import { requireUser } from "@/lib/auth";

export default async function NotificationsPage({ params }: { params: Promise<{ locale: string }> }) {
  const { locale } = await params;
  const user = await requireUser(locale);
  // Each notification opens in the account's workspace (QC-290); delivery settings live in Settings
  return <NotificationList roles={user.roles} />;
}
