export interface Person{
	FromId: string,
	FirstName: string,
	LastName: string
}

export interface EpubDownloadContract {
	FileId: string,
	Person: Person
}