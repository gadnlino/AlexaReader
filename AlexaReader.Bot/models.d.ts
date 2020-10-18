export interface User{
	FromId: string,
	FirstName: string,
	LastName: string
}

export interface DownloadEpubContract {
	FileId: string,
	User: User
}