# SQLAzureBacpac

This is a little application that you can use to make .bacpac files (and store locally) of databases hosted in Windows Azure.

##needed input
- sql server name
- sql server username
- sql server password
- storage account key
- storage container name (where the bacpacs will be temporarily saved)
- database name (not yet possible to select the database which you want to download)

##how it works
A bacpac is made via the dacserver webservice. It is then saved to the blobstorage of your windows azure account.
The application will download this file to the location that you entered and will delete the bacpac that exists in your blob storage.

##dependencies
* Caliburn.Micro
* Ninject
* Windows.Azure.Storage
