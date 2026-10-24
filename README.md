# Azure SQL Database Hyperscale — Microsoft Mechanics Examples

Demo code and runbooks from the **Microsoft Mechanics** show on
[Azure SQL Database Hyperscale](https://learn.microsoft.com/azure/azure-sql/database/service-tier-hyperscale).
Each folder is a self-contained example built around **Caldova Health**, a fictional
healthcare network, and has its own README with prerequisites, deployment steps, and a
demo runbook.

## Examples

| Folder | What it shows |
|---|---|
| [startandgrowhyperscale](startandgrowhyperscale/README.md) | Start on a Hyperscale serverless database, then bring acquired providers' databases into a Hyperscale elastic pool and query the whole network while storage grows automatically. |
| [handsfreeindexing](handsfreeindexing/README.md) | Automatic tuning creates a covering index, then Automatic Index Compaction repacks it after insert/delete activity leaves it sparse. |
| [caldovaresearch](caldovaresearch/README.md) | Evidence search over a PubMed Central corpus with vector search, scaling from a serverless pilot to primary and named-replica targets without changing the application. |
| [caldovaevidenceagent](caldovaevidenceagent/README.md) | A biomedical evidence agent using Hyperscale native embeddings and DiskANN vector search, Data API builder SQL MCP Server tools, and a Microsoft Foundry hosted agent. |

A GitHub Copilot skill that guides building and presenting the start-and-grow demo is in
[.github/skills/start-and-grow-hyperscale-demo](.github/skills/start-and-grow-hyperscale-demo/SKILL.md).

## Getting started

1. Clone this repository.
2. Open the folder for the example you want to run.
3. Follow that folder's README and `DEMO-RUNBOOK.md` (where provided).

The examples deploy real Azure resources to your subscription and those resources incur
cost. Delete them when you are finished. The examples contain no patient data.

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to
agree to a Contributor License Agreement (CLA) declaring that you have the right to, and
actually do, grant us the rights to use your contribution. For details, visit
<https://cla.opensource.microsoft.com>.

This project has adopted the
[Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the
[Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or
comments.

## Security

See [SECURITY.md](SECURITY.md) for how to report security issues.

## License

Licensed under the [MIT License](LICENSE).

## Trademarks

This project may contain trademarks or logos for projects, products, or services.
Authorized use of Microsoft trademarks or logos is subject to and must follow
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/legal/intellectualpropertyandcustomeragreements/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause
confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos is
subject to those third-party's policies.
