# FASTER обеспечивает пропускную способность до 160 миллионов операций в секунду на одном сервера
https://docs.google.com/document/d/1FbFRFavQcQjm_1z6MWCEydu9QT08d23Dyi6M6YWWkIU/edit

Если данные умещаются в ОЗУ в тестах на одном сервере обеспечивается производительность на уровне 160 млн операций в секунду. Для обеспечения целостности предоставляется специально разработанная в Microsoft Research новая схема восстановления записей, которая отличается от других решений более высокой производительностью, ценой незначительного повышения задержек при фиксации коммитов.
БД отлично адаптирована для применений, активность в которых строится на последовательности операций чтения, изменения и перезаписи данных в БД. Для достижения высокой интенсивности операций обновления и экономии памяти в FASTER используется архитектура на основе гибридного лога записей (HybridLog), который комбинирует структуру в виде хэша, допускающую замену по месту существующих записей в оперативной памяти, с организацией хранения на диске в форме только дополняемого лога. Особенностью HybridLog также является осуществление буферизации и хранения на уровне отдельных записей, а не блоков фиксированного размера. 

 FASTER supports data larger than memory, by leveraging fast external storage.
 https://www.microsoft.com/en-us/research/project/FASTER/#!downloads

FASTER используется архитектура на основе гибридного лога записей (HybridLog), который комбинирует структуру в виде хэша, допускающую замену по месту существующих записей в оперативной памяти, 
https://www.microsoft.com/en-us/research/uploads/prod/2018/03/faster-sigmod18.pdf


# Introduction

Managing large application state easily and with high performance is one of the hardest problems
in the cloud today. We present FASTER, a new concurrent key-value store designed for point lookups 
and heavy updates. FASTER supports data larger than memory, by leveraging fast external storage. 
What differentiates FASTER are its cache-optimized index that achieves very high performance — up
to 160 million operations per second when data fits in memory; its unique “hybrid record log” design
that combines a traditional persistent log with in-place updates, to shape the memory working set 
and retain performance; and its architecture as an component that can be embedded in cloud apps. FASTER
achieves higher throughput than current systems, by more than two orders of magnitude, and scales better 
than current pure in-memory data structures, for in-memory working sets. FASTER also offers a new consistent
recovery scheme that achieves better performance at the expense of slightly higher commit latency.

# Getting Started

Go to [our website](http://aka.ms/FASTER) for more details and papers.

# Build and Test

For C#, see [here](https://github.com/Microsoft/FASTER/tree/master/cs).

For C++, see [here](https://github.com/Microsoft/FASTER/tree/master/cc).

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
