Сайт проекта.   
https://www.microsoft.com/en-us/research/project/FASTER/   

Архитектура FASTER обеспечивает пропускную способность до 160 миллионов операций в секунду на одном сервера   
https://drive.google.com/drive/folders/1KC-aqc0pq1thDYrKMJGZ_SGI9FjKQ0zm   

https://docs.google.com/document/d/1FbFRFavQcQjm_1z6MWCEydu9QT08d23Dyi6M6YWWkIU/   

Если данные умещаются в ОЗУ в тестах на одном сервере обеспечивается производительность на уровне 160 млн операций в секунду. Для обеспечения целостности предоставляется специально разработанная в Microsoft Research новая схема восстановления записей, которая отличается от других решений более высокой производительностью, ценой незначительного повышения задержек при фиксации коммитов. БД отлично адаптирована для применений, активность в которых строится на последовательности операций чтения, изменения и перезаписи данных в БД. Для достижения высокой интенсивности операций обновления и экономии памяти в FASTER используется архитектура на основе гибридного лога записей (HybridLog), который комбинирует структуру в виде хэша, допускающую замену по месту существующих записей в оперативной памяти, с организацией хранения на диске в форме только дополняемого лога. Особенностью HybridLog также является осуществление буферизации и хранения на уровне отдельных записей, а не блоков фиксированного размера    

FASTER supports data larger than memory, by leveraging fast external storage.   
https://www.microsoft.com/en-us/research/project/FASTER/#!downloads   

FASTER используется архитектура на основе гибридного лога записей (HybridLog), который комбинирует структуру в виде хэша, допускающую замену по месту существующих записей в оперативной памяти, https://www.microsoft.com/en-us/research/uploads/prod/2018/03/faster-sigmod18.pdf


<p align="center">
  <img src="https://raw.githubusercontent.com/microsoft/FASTER/master/docs/assets/images/faster-logo.png" alt="FASTER logo" width="600px" />
</p>
  
[![NuGet](https://img.shields.io/nuget/v/Microsoft.FASTER.Core.svg)](https://www.nuget.org/packages/Microsoft.FASTER.Core/)
[![Build Status](https://dev.azure.com/ms/FASTER/_apis/build/status/Microsoft.FASTER?branchName=main)](https://dev.azure.com/ms/FASTER/_build/latest?definitionId=8&branchName=main)
[![Gitter](https://badges.gitter.im/Microsoft/FASTER.svg)](https://gitter.im/Microsoft/FASTER?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

# Introduction

Managing large application state easily, resiliently, and with high performance is one of the hardest
problems in the cloud today. The FASTER project offers two artifacts to help tackle this problem.

* **FASTER Log** is a high-performance concurrent persistent recoverable log, iterator, and random 
reader library in C#. It supports very frequent commit operations at low latency, and can quickly saturate 
disk bandwidth. It supports both sync and async interfaces, handles disk errors, and supports checksums.

* **FASTER KV** is a concurrent key-value store + cache (available in C# and C++) that is designed for point 
lookups and heavy updates. FASTER supports data larger than memory, by leveraging fast external 
storage (local or cloud). It also supports consistent recovery using a fast non-blocking checkpointing technique 
that lets applications trade-off performance for commit latency.

Both FASTER KV and FASTER Log offer orders-of-magnitude higher performance than comparable solutions, on standard
workloads. Start learning about FASTER, its unique capabilities, and how to get started at our official website:

[aka.ms/FASTER](https://aka.ms/FASTER)

# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
