import { redirect } from "@/i18n/navigation";

export default async function HeadPortalPage({
  params,
}: PageProps<"/[locale]/head">) {
  const { locale } = await params;
  redirect({ href: "/head/programme-overview", locale });
}
