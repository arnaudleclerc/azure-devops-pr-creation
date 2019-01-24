# Azure Samples - Creation of a PR on Azure DevOps

This repository contains a sample of a single Azure Function creating a PR on Azure DevOps. This function can for example be called during a release process using a Gate. The work items attached to the source branch are retrieved and attached to the PR. Commits messages are added as description of the PR.

## Configuration

| Key | Description |
| --- | --- |
| OrganizationURL | Address of your instance of Azure DevOps |
| AccessToken | Private Access Token to connect to Azure DevOps |
| Useremail | E-Mail of the user connecting to Azure DevOps |

## Calling the function

The Azure Function expects a POST request. A following body is expected :

`
{
	"repository": "name of the git repository",
	"branches": {
		"source": "name of the source branch",
		"target": "name of the target branch"
	},
	"releaseId": "id of the release"
}
`

For example : 

`
{
	"repository": "MyRepository",
	"branches": {
		"source": "develop",
		"target": "master"
	},
	"releaseId": "1"
}
`