import { ProfilePageContent } from "@/components/profile/profile-page-content"

interface PublicProfilePageProps {
  params: Promise<{ id: string }>
}

export default async function PublicProfilePage({
  params,
}: PublicProfilePageProps) {
  const { id } = await params

  return <ProfilePageContent profileId={id} />
}
