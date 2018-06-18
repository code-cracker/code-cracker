# Code Cracker

Uma biblioteca de analizadores (analyzer library) para C# e VB que usam [Roslyn](https://github.com/dotnet/roslyn) para refatorações, análises de código e outros detalhes. 

De uma olhada no site oficial [code-cracker.github.io](http://code-cracker.github.io). Lá você irá encontrar informações sobre como contribuir, 
nossa lista de tarefas, definição para "done", definição para "ready" entre outras coisas.

[![Build status](https://ci.appveyor.com/api/projects/status/h21sli3jkumuswyi?svg=true)](https://ci.appveyor.com/project/code-cracker/code-cracker)
[![Nuget count](https://img.shields.io/nuget/v/codecracker.CSharp.svg)](https://www.nuget.org/packages/codecracker.CSharp/)
[![License](https://img.shields.io/github/license/code-cracker/code-cracker.svg)](https://github.com/code-cracker/code-cracker/blob/master/LICENSE.txt)
[![Issues open](https://img.shields.io/github/issues-raw/code-cracker/code-cracker.svg)](https://huboard.com/code-cracker/code-cracker/)
[![Coverage Status](https://img.shields.io/coveralls/code-cracker/code-cracker/master.svg)](https://coveralls.io/r/code-cracker/code-cracker?branch=master)
[![Source Browser](https://img.shields.io/badge/Browse-Source-green.svg)](http://ccref.azurewebsites.net)

Você pode encontrar este documento nas seguintes línguas

[![English](https://img.shields.io/badge/language-english-blue.svg)](https://github.com/code-cracker/code-cracker/blob/master/README.md)
[![Brazilian Portuguese](https://img.shields.io/badge/language-brazilan%20portuguese-brightgreen.svg)](https://github.com/code-cracker/code-cracker/blob/master/README.pt.md)


Este é um projeto da comunidade, *free* e *open source*. Todos estão convidados para contribuir, forkar, compartilhar e usar o código.

## Features

A lista de *features* está documentada aqui: http://code-cracker.github.io/diagnostics.html 

#### Design
Code | Analyzer | Gravidade | Descrição 
-- | -- | -- | --
[CC0003](http://code-cracker.github.io/diagnostics/CC0003.html) | CatchEmptyAnalyzer | Warning | Declarações de catch sem Exption como um argumento não é recomendado. Considere adicionar uma classe Exception à instrução catch.
[CC0004](http://code-cracker.github.io/diagnostics/CC0004.html) | EmptyCatchBlockAnalyzer | Warning | Um bloco catch vazio suprime todos os erros e não deve ser usado. Se o erro for esperado, considere registrá-lo ou alterar o fluxo de controle de modo que seja explícito.
[CC0016](http://code-cracker.github.io/diagnostics/CC0016.html) | CopyEventToVariableBeforeFireAnalyzer | Warning | Os eventos devem sempre ser verificados para null antes de serem invocados. Como em um contexto multi-threading, é possível que um evento seja anulado entre o momento em que ele é verificado como não-nulo e, no momento em que é gerado, o evento deve ser copiado para uma variável temporária antes da verificação.
[CC0021](http://code-cracker.github.io/diagnostics/CC0021.html) | NameOfAnalyzer | Warning | Em C # 6, o operador nameof () deve ser usado para especificar o nome de um elemento de programa em vez de um literal de string, pois ele produz um código mais fácil de refatorar.
[CC0024](http://code-cracker.github.io/diagnostics/CC0024.html) | StaticConstructorExceptionAnalyzer | Warning | O construtor estático é chamado antes da primeira vez que uma classe seja usada, mas o chamador não controla exatamente quando. A exceção lançada, neste contexto, força os chamadores a usar o bloqueio ‘try’ em torno de qualquer uso da classe e deve ser evitado.
[CC0031](http://code-cracker.github.io/diagnostics/CC0031.html) | UseInvokeMethodToFireEventAnalyzer | Warning | Em C # 6, um *delegate* pode ser chamado usando o operador de propagação nulo ou null-propagating operator (?.) E seu método de invocação para evitar lançar uma exceção NullReference quando não houver nenhum método anexado ao *delegate*.

## Instalação

Você pode usar o CodeCracker de duas formas: como um *analyzer library* que você instala com o Nuget dentro do seu projeto ou como uma extensão do Visual Studio. 
A maneira como você deseja usá-lo depende do cenário em que você está trabalhando. Você na maioria das vezes vai optar por usar como um pacote Nuget.

Se você quiser que os analisadores funcionem durante a sua construção, e que gerem avisos e erros durante o build, também nos build servers, então você vai querer
usar o pacote Nuget. O pacote está disponível no nuget([C#](https://www.nuget.org/packages/codecracker.CSharp),
[VB](https://www.nuget.org/packages/codecracker.VisualBasic)).

Se você quer ser capaz de configurar quais analisadores estão sendo usados em seu projeto, e quais você irá ignorar, commitar essas mudanças no controle de código e compartilhar com sua equipe, então você também quer o pacote Nuget.

Instalação pelo Nuget, para a versão C#: 

```powershell
Install-Package CodeCracker.CSharp
```

ou para a versão Visual Basic:

```powershell
Install-Package CodeCracker.VisualBasic
```

Também é possível instalar pelo Gerenciador de Pacotes (Package Manager) dentro do Visual Studio.

Além das versões específicas para cada linguagem de programação, também temos uma versão para ambas, chadama somente de `CodeCracker` sem sufixo, mas
não faz sentido usarem essa versão já que provavelmente trabalhará com uma das duas linguagens.

Se você quer os build alpha, o que *builda* a cada push, adicione https://www.myget.org/F/codecrackerbuild/ para o seu nuget feed. 
Nós apenas enviamos versões completas para o Nuget.org, e commit builds para Myget.org.

Se você quer o analyzer globalmente, ou seja, que funciona toda vez que abre um projeto no Visual Studio, então você quer a extensão.

Procure pela extensão na **Galeria de Extensões** do Visual Studio.([C#](https://visualstudiogallery.msdn.microsoft.com/ab588981-91a5-478c-8e65-74d0ff450862),
[VB](https://visualstudiogallery.msdn.microsoft.com/1a5f9551-e831-4812-abd0-ac48603fc2c1)).

para build a partir do fonte:

```shell
git clone https://github.com/code-cracker/code-cracker.git
cd CodeCracker
msbuild
```
Em seguida, adicione uma referência ao CodeCracker.dll de dentro das referências, no Visual Studio.

Se voce quer usar o CodeCracker em todos os projetos, instale a extensão para Visual Studio ([C#](https://visualstudiogallery.msdn.microsoft.com/ab588981-91a5-478c-8e65-74d0ff450862), [VB](https://visualstudiogallery.msdn.microsoft.com/1a5f9551-e831-4812-abd0-ac48603fc2c1)). Se você quer usar o CodeCracker só para um projeto, instale o pacote Nuget como descrevemos acima.

## SonarQube Plugin

CodeCracker tem um plugin para o SonarQube que pode ser baixado através deste link [Plugins HomePage](http://docs.sonarqube.org/display/PLUG/Other+Plugins). 

## Contribuindo [![Open Source Helpers](https://www.codetriage.com/code-cracker/code-cracker/badges/users.svg)](https://www.codetriage.com/code-cracker/code-cracker)

A principal IDE suportada para desenvolvimento é o Visual Studio 2017. Não temos mais suporte para VS 2015.

Perguntas, comentários, report de bugs e pull requests são bem-vindos.
Os Reports de bugs devem incluir o passa-a-passo (incluindo o código). Melhor ainda, utilizem o formato de uma pull request. Antes de você começar a trabalhar em uma issue existente, verifique se a mesma já não foi atribuida para alguém, caso isso tenha acontecido, converse com a pessoa. 

Verifique também o board do [projeto](https://huboard.com/code-cracker/code-cracker/) e verifique se já não estão trabalhando na tarefa (estará marcada como `Working` tag). Se a tarefa estiver livre, antes de você começar, verifique se o item tem a tag `Ready`. Se a issue estiver com a tag `Working` (*working* na raia de trabalho) e não houver atribuição então a tarefa ainda não começou a ser trabalhada por alguém do **core team**. Verifique a descrição da issue para procurar pelo responsável (se não estiver lá, você encontrará nos comentários). Nos estamos adicionando pessoas que querem contribuir com o projeto ao `Contributors` team assim nos podemos sempre atribuir os *Contributors* para as issues, provavelmente na sua primeira contribuição você será adicionado neste time.

A forma mais fácil de começar é pesquisando pelas issues com a tag [up for grabs](https://github.com/code-cracker/code-cracker/labels/up-for-grabs). Você pode pedir para trabalhar com qualquer uma delas, leia abaixo para ver **como**. Você também pode fazer a triagem das issues que podem incluir a reprodução dos reports de bug, ou perguntando sobre informações importantes como o número das versões ou instruções para reprodução. Se você quiser iniciar a triagem das issues, uma forma fácil de começar é [subscribe to code-cracker on CodeTriage](https://www.codetriage.com/code-cracker/code-cracker).

Se você está iniciando com Roslyn e quer contribuir mas sente que ainda não está preparado para começar trabalhando com a criação de analyzers ou code fixes, você pode começar ajudando com as áreas que são menos demandadas. Nós identificamos algumas:

* Fixing bugs

  Ainda exige conhecimento dos componentes internos do Roslyn, mas é mais fácil do que criar um novo 
  analyzer ou *fix bugs*. Procure por [bugs that are up for grabs](https://github.com/code-cracker/code-cracker/issues?utf8=%E2%9C%93&q=is%3Aopen+label%3Abug+label%3Aup-for-grabs).

* Documentação

  Estamos documentado todos os analyzers no [CodeCracker user site](http://code-cracker.github.io/diagnostics.html).
  Existem muitos analyzers e correções de código para documentar.

* Traduzindo
  
  Nós estamos começando a traduzir os analyzers e mensagens para outras línguas. Se você gostaria de ver o CodeCracker na sua
  língua nativa venha nos ajusdar, crie uma issue e comece a tradução. Se você quer ajudar com uma tradução já em andamento,
  comente na issue existente oferecendo sua ajuda. Nós também precisamos atualizar os analyzers existentes.

## Issues e task board

* O task board está em [Huboard](https://huboard.com/code-cracker/code-cracker/).
* Você também pode encontrar no [Github backlog](https://github.com/code-cracker/code-cracker/issues) diretamente.

### Definição de Ready (DoR)

Só podemos começar a trabalhar em um item depois que o *backlog item* estiver marcado como *ready*. 
Nós definimos *ready* quando:

1. Quando temos a maioria dos cenários/casos de teste definidos na issue aqui no Github.
2. Se no backlog item tiver um ou mais analyzers então
  1. O nível de alerta (warning level) do analyzer deve estar definido na descrição da issue (`Hidden`, `Information`, `Warning`, ou `Error`).
  2. O *diagnostic* informado na issue já deve ter o id definido no formato `CC0000`.
3. Se houver um *code fix* então a categoria deve estar definida na descrição da issue. As categorias suportadas estão listadas no arquivo `SupportedCategories.cs`.
4. Um dos mantenedores devem ter verificado o item (não necessariamente o mesmo que escreveu a issue e/ou os casos de teste).

O primeiro item é importante para definirmos claramente o que iremos construir. O último 
é igualmente importante para não criarmos algo que não será útil, que irá atrapalhar os usuários ou que 
será um disperdício de esforço.

Vejam exemplos nas issues [#7](https://github.com/code-cracker/code-cracker/issues/7)
e [#10](https://github.com/code-cracker/code-cracker/issues/10).

### Níveis de Severidade

Estes são os 4 níveis de severidade suportados no Roslyn e como eles são entendidos no projeto Code Cracker:

1. **Hidden**: Somente utilizados para *refactorings*. Leia a issue [#66](https://github.com/code-cracker/code-cracker/issues/66) (inclusive os comentários) para entender melhor.
2. **Info**: Uma maneira alternativa (ex: substituindo *for* pelo *foreach*). Quando claramente for uma questão de opinião e/ou a quando qualquer uma das formas podem ser consideradas corretas.
3. **Warning**: Código que pode/deve ser melhorado. Estes são os *code smells* e provavelmente estão escritos de forma errada, mas existem situações que o pattern pode ser desconsiderado.
4. **Error**: Claramente um erro. (ex: *throwing* ArgumentException com um parâmetro inexistente). Não há nenhuma situação em que esse código possa estar correto. Não há diferenças de opinião.

Também é possível saber as definições da [própria  Microsoft](http://source.roslyn.codeplex.com/#Microsoft.CodeAnalysis/Diagnostic/DiagnosticSeverity.cs,e70281df673d47f6,references) de como eles interpretam estes níveis.

### Definição de Done (DoD)

O DoD ainda está evoluindo. Até o momento seguimos o checklist abaixo:

1. passando pelo Build.
2. Os analyzers tem testes, com as correções dos códigos e *refactoring*.
3. Todos os testes devem passar.
4. Os Analyzers devem seguir os padrões para nomes definidos no *guidelines*
  1. Sempre seguir o padrão `<featurename>Analyzer`.
  2. Sempre adicionar o id do *diagnostic* no arquivo `DiagnosticIds.cs`.
5. As correções de códigos devem seguir os *guidelines* para definição dos nomes
  1. Sempre seguir o padrão `<featurename>CodeFixProvider`.
  2. Sempre usar o mesmo id do *diagnostic* no arquivo `DiagnosticIds.cs`, a menos que você esteja escrevendo uma correção de código para um id do *diagnostic* levantado pelo próprio compilador C # (com as iniciais `CS`).
6. Correção de todos os cenários (Correção para todos os cenários no documento, no projeto and na *solution*). Talvez precise escrever um `FixAllProvider`. Verifique o `DisposableVariableNotDisposedFixAllProvider` como exemplo.
7. Siga os padrões de codificação presentes nos arquivos de código do projeto.
8. Funcionando no Visual Studio.
9. Usar strings localizaveis.

### Comece a Trabalhar

Quando estiver pronto e acordado por qualquer um do *core team*, apenas informe em 
um comentário que você pretende começar a trabalhar neste item e mencionar qualquer ou todos
os mantenedores (use @code-cracker/owners) assim eles podem *tegar* a issue corretamente e move-la no board.

Se você não está familiarizado com o funcionamento do Github, talvez queira verificar o [Github guides](https://guides.github.com/), em 
especial o [Github flow](https://guides.github.com/introduction/flow/). O
[GitHub for the Roslyn Team video](http://channel9.msdn.com/Blogs/dotnet/github-for-the-roslyn-team) pode ajudá-lo também, e
também explica alguns conceitos do Git.

Para começar a trabalhar, *fork* o projeto no Github com sua própria conta e copie-o **de lá, sua própria conta**.
To start working fork the project on Github to your own account and clone it **from there**. Não clone
direto do repositório do CodeCracker. Antes de iniciar a codificação, crie uma nova *branch* e nomeie-a de uma forma que tenha
sentido para a issue que você estará trabalhando. Não trabalhe na *branch* `master` porque isso pode tornar 
as coisas mais difíceis se você tiver que atualizar sua *pull request* ou seu repositório mais tarde,
suponha que a sua *branch* `master` é sempre igual a *branch* `master` do repositório principal, e o seu código em uma *branch* diferente.

Na mensagem do seu *commit*, lembre-se de mencionar o número da issue usando o sinal de cerquilha (#) na frente do número. Evite fazer pequenos *commits*
a menos que sejam significativos. Para a maioria dos analyzers e correções de código (code fixes), um único *commit* deve ser o suficiente. Se preferir 
trabalhar com muito commits, no final faço o processo de *squash*.

Faça com que suas primeiras linhas de *commit* signifiquem algo, especialmente a primeira.
[Aqui](https://robots.thoughtbot.com/5-useful-tips-for-a-better-commit-message) e
[aqui](http://tbaggery.com/2008/04/19/a-note-about-git-commit-messages.html) existem algumas dicas de uma boa  primeira linha/mensagem de
*commit*.

**Não**, em qualquer circunstância, reformate o código para adequá-lo aos seus padrões. Siga os padrões do projeto,
e se você não concordar com os padrões, discuta abertamente com a comunidade Code Cracker. Além disso, evite o fim da linha
espaços em branco a todo custo. Mantenha seu código limpo.

Sempre escreva testes de unidade para seus analyzers, correções de código (code fixes).

### Pull Request

Quando terminar, baixe as últimas mudanças da *branch* `master` do repositório principal do CodeCracker e integre-as a sua *branch*.

Você pode fazer isso na linha de comando:
````bash
# adicione o repositório principal com o nome de `codecracker`
git remote add code-cracker https://github.com/code-cracker/code-cracker.git
# vá para a branch master
git checkout master
# faça o download das últimas alterações feitas na branch master do repositório principal
git pull code-cracker master
# volte para a sua branch de trabalho
git checkout <youbranchname>
# integre as mudanças
git merge master
# resolva os conflitos de integração
````

Você pode resolver os conflitos no seu editor de textos favorito, ou, se você estiver usando o Visual Studio, também poderá usa-lo para está tarefa.
O Visual Studio na verdade apresenta os conflitos de uma muito simples e boa para resolve-los.
Além disso, no passo `volte para a sua branch de trabalho` você pode voltar a usar o Visual Studio para controlar o git, se você preferir.

Se você conhecer um pouco mais de git, você pode usar o comando `rebase` ao invés do `merge`. Caso contrário, tudo bem se fizer um `merge`. 
Quando as suas alterações estiverem atualizadas com a 
branch `master` então você precisará envia-lo para o seu repositório remoto (no GitHub) e então você estará pronto para criar
um [pull request](https://help.github.com/articles/using-pull-requests/). Não esqueça de mencionar a issue que original essa PR e
capriche na sua mensagem da PR mantendo ela clara e objetiva. Se quando você criar a `pull request` no GitHub receber um retorno informando
alguma divergência, isso significa que a sua PR não pode ser mesclada porque ainda existem conflitos com a branch `master`. Corrija os conflitos,
envie para o seu repositório pessoal (aquele forkado no início). Isso irá automaticamente atualizar a sua PR.
O mantenedores do projeto não deverão resolver conflitos de `merge`, você quem precisa fazer isso.

Depois que a sua `pull request` for aceita você pode deletar a sua *branch* local, claro, se quiser. Lembre-se de atualizar a *branch* `master` assim
poderá continuar contribuindo no futuro. e obrigado! :)

Se a sua *pull request* não for autorizada tente entender o motivo. Não é incomum que PRs sejam rejeitadas e depois de algumas
discussões e correções elas são aceitas. Trabalhe com a comunidade para obter o melhor código possível. and Obrigado!

### Regras para contribuição

* Todo *pull request* deve ter testes de unidade. PRs sem testes serão negadas (denied) sem verificar qualquer outro item;
* Tem que *buildar* e todos os testes devem passar;
* Deve mencionar a issue correspondente a PR no GitHub;
* Não altere nenhum código além do alinhado na issue correspondente ao PR;
* Siga os padrões de codificação (coding standars) já em vigor no projeto;
* Uma *code issue* de cada vez por pessoa (issues bloqueadas não contam);
* Seu *pull request* será comentado pelo bot Coveralls. Certifique-se de que a cobertura do código não diminuiu significativamente. Idealmente, deveria subir.

Se você trabalhou em algo que você ainda não discutiu com os mantenedores
existe uma chance de o código ser negado porque eles podem achar que o *analyzer* / *fix* não é necessário, duplicado ou algum outro motivo.
Os mantenedores são facilmente acessados ​​através do Twitter ou GitHub. Antes de codificar alinhe com os mantenedores.

Mudanças de código pequenas ou atualizações fora dos arquivos de código serão eventualmente feitas pela equipe principal, diretamente no `mestre`, sem um PR.


## Maintainers/Core team

* [Giovanni Bassi](http://blog.lambda3.com.br/L3/giovannibassi/), aka Giggio, [Lambda3](http://www.lambda3.com.br), [@giovannibassi](https://twitter.com/giovannibassi)
* [Elemar Jr.](http://elemarjr.net/), [Promob](http://promob.com/), [@elemarjr](https://twitter.com/elemarjr)
* [Carlos dos Santos](http://carloscds.net/), [CDS Informática](http://www.cds-software.com.br/), [@cdssoftware](https://twitter.com/cdssoftware)

Os contribuidores podem ser encontrados aqui: [contributors](https://github.com/code-cracker/code-cracker/graphs/contributors) página no Github.

### Quais são as responsabilidades dos mantenedores ?

Os mantenedores devem:

* Commitar regularmente;
* Trabalhar regularmente em tarefas de manutenção do projetos, como (mas não limitado a)
  * participart dos encontros,
  * revisar pull requests,
  * criar e discutir nas *issues* do projeto;

Para fazer parte da equipe principal, é preciso ser convidado. Os convites só acontecem se toda a equipe principal concordar.

Se um membro da equipe principal não estiver ativo por pelo menos dois meses, ele provavelmente será removido da equipe principal.

## Contato

Por favor veja a nossa [página de contatos](http://code-cracker.github.io/contact.html).

## Licença

Este software é open source, licenciado sob a licença Apache, versão 2.0.
Veja [LICENSE.txt](https://github.com/code-cracker/code-cracker/blob/master/LICENSE.txt) para os detalhes.
Confira os termos da licença antes de contribuir, *forcar*, copiar ou fazer qualquer coisa com o código.
with the code. Se você decidir contribuir, você concorda em conceder direitos autorais de toda a sua contribuição para este projeto, e concorda em mencionar claramente se não concordar com estes termos. Seu trabalho será licenciado com o projeto no Apache V2, ao longo do restante do código.
