# CSharp for Revit — contexto do projeto

Add-in do Bruno: estudo (curso guRoo) e ferramentas de produção. **Siga a skill `revit-plugin-dev`** para arquitetura, padrões de
código, receitas e o protocolo "passo a passo, implementando juntos". Este arquivo guarda só o **estado deste
projeto**: o que existe, onde está e o que está pendente. Atualize-o ao final de cada feature.

## Fatos rápidos

| Item | Valor |
|---|---|
| Solução | `C:\DEV\CSharp for Revit\CSharp for Revit\CSharp for Revit.sln` |
| Projeto | `C:\DEV\CSharp for Revit\CSharp for Revit\CSharp for Revit\` |
| SDK | `Nice3point.Revit.Sdk/6.2.2` (`DeployAddin`, `LaunchRevit`, `UseWPF`, `UseWindowsForms`) |
| Configurações | `Debug.R23…R27` e `Release.R23…R27` (o Bruno usa principalmente **R25**, .NET 8) |
| RootNamespace | `CSharp_for_Revit` |
| Aba do Ribbon | `Globals.AddinName` = **"C# for Revit"** |
| Deploy | `%AppData%\Autodesk\Revit\Addins\2025\CSharp for Revit\` + `CSharp for Revit.addin` (`Type="Application"`) |
| Pacotes extras | ClosedXML 0.105.1, WindowsAPICodePack-Shell 1.1.1, CommunityToolkit.Mvvm 8.4.2 |

**Compilar e testar:** fechar todos os Revit → Ctrl+Shift+B no VS → abrir o Revit (ou F5 em Debug.R25 para
depurar). Mudança que "não aparece" quase sempre é DLL antiga: confira a data da DLL na pasta de deploy.

## Ribbon atual

Prefixo das classes: `CSharp_for_Revit.Commands.General.`

| Painel | Item | Texto | Classe (nome-base) | Availability |
|---|---|---|---|---|
| Test Panel | push | Test Button | `Command_Button` | ZeroDoc |
| Test Panel | pulldown | Test Pulldown | `Command_Pulldown` (sem classe) | — |
| | ↳ push | Pulldown Button 1/2/3 | `Command_1Button` / `2` / `3` | padrão do Revit |
| Test Panel | 3 pulldowns empilhados | Stack 1/2/3 → Button 1/2/3 | `Command_Stack1/2/3` | padrão |
| Test Panel | split | Split 1/2/3 | `Command_Split1/2/3` | padrão |
| Tools | pulldown | Revision | `Revision_Button` | — |
| | ↳ push | Revision1 | `Revision1_Button` (revisão em folhas, com progresso) | Project |
| Tools | pulldown | Workset | `Workset_Button` | — |
| | ↳ push | Create workset(s) | `Workset1_Button` | Workshared |
| Tools | pulldown | Select | `Select_Button` | — |
| | ↳ push | Select titleblocks on sheets | `Select1_Button` | Selection_SheetsOnly |
| | ↳ push | Select rooms | `Select2_Button` | Project |
| | ↳ push | Select hidden elements | `Select3_Button` | Project |
| Tools | pulldown | Import | `Import_Button` | — |
| | ↳ push | Import sheets from Excel | `Import1_Button` | Project |
| Tools | pulldown | Export | `Export_Button` | — |
| | ↳ push | Export sheets to PDF | `Export1_Button` | Project |
| Tools | pulldown | Dimension | `Dimension_Button` (ícone provisório = cópia do `Command_Button`) | — |
| | ↳ push | Stacked dimensions (vertical) | `Dimension1_Button`: versão de teste que **cria** as cotas baseline que medem X, para tubos verticais **e risers** (horizontais ignorados), tipo `CTS - Linear - 3/32" Arial - Line Leader - Inline Text BASELINE`, linha de cota sobre os tubos. Só tubos CID 2041: os da seleção ou, sem seleção, todos da vista. A validação (filtro da seleção) foi testada em 2026-10-19; a criação de cotas *ainda não foi compilada*. | Project |
| Tools | push | Testing | `Testing_Button` (lê `C:\Users\User\Desktop\Pasta1.xlsx`) | Project |

## Catálogo — reaproveite antes de criar

**Forms**
- `Custom`: `Message`, `SelectFromDropdown`, `SelectFromList`, `SelectFilePaths`, `SelectFolder`, `Completed`,
  `Cancelled`, `Error`, constante `FILTER_EXCEL`.
- `FormResult`: `Object`, `Objects`, `Cancelled`, `Valid`, `Affirmative`, `Validate(...)`.
- `Bases`: `BaseDropdown`, `BaseListView`, `BaseProgressBar` (`Increment`, `CancelCheck(t)`, `Commit(t)`).

**General**
- `Globals`: `RegisterProperties`, `RegisterTooltips`, `AddinName`, `Assembly`, `AssemblyPath`, `CtlApp`, `UiApp`,
  `RevitVersion`, `Tooltips`.
- `Availability` + `AvailabilityNames`: `Disabled`, `ZeroDoc`, `Project`, `Family`, `Workshared`, `Selection`,
  `Selection_SheetsOnly`.
- `SyncTimer`: `Register(ctlApp)`, `DeRegister(ctlApp)` (ver pendência 1).

**Utilities**
- `Connector_Utilities`: `GetConnectors` (FabricationPart / MEPCurve / FamilyInstance), `GetEndPoints` (só
  `ConnectorType.End`), `GetDirection` (vetor unitário, `null` se não tiver 2 pontas). *Compilado e testado pelo Bruno em 2026-10-19.*
- `FabricationPart_Utilities`: `GetFabricationParts(doc, view = null, List<FabricationCid> cids = null)` (categoria
  `OST_FabricationPipework`, filtro por `ItemCustomId`), `GetFabPartInfo(part, view)`, `GetFabPartInfos`,
  `GetPositionInView(info, axis)` (X ou Y do ponto médio; `NaN` se inválido), `FilterByOrientation`, `FabPartKey`. *Compilado e testado pelo Bruno em 2026-10-19.*
  `FilterFabricationParts(elements, cids)` (mesma regra para a seleção) e `FilterByCid(parts, cids)` (único lugar
  da regra de CID) testados em 2026-10-19. *Ainda não compilado:* `RemoveClosePositions(infos, axis, toleranceFeet)`
  (fica o maior Ø; empate → o mais comprido). Arquitetura planejada em `UML-Cotas-Escalonadas.md`.
- `Grid_Utilities`: `GetGrids(doc, view = null)` (só retos), `GetGridOrientation`, `GetGridPosition` (X ou Y da tela;
  `NaN` se diagonal), `GetNearestGrid(info, grids, view, gridOrientation)` → `GridMatchInfo`, `GridMatchKey` (distância nas unidades do
  projeto) (`Grid`, `DistanceFeet`,
  `IsOnPositiveSide`). Riser chama duas vezes (Vertical e Horizontal). *Compilado e testado pelo Bruno em 2026-10-19.*
- `View_Utilities`: `ORIENTATION_TOLERANCE_DEGREES = 1.0`, `ToViewCoordinates` (X = direita da tela, Y = topo,
  Z = profundidade), `GetOrientation` → `OrientationInView`. *Compilado e testado pelo Bruno em 2026-10-19.*
  *Ainda não compilado:* `FromViewCoordinates` (o inverso do `ToViewCoordinates`).
- `Dimension_Utilities` (*ainda não compilado*): `GetDimensionTypeByName`, `GetCenterlineEndPointReference(element, view)` (ponto do eixo, para risers;
  se falhar, o `CreateBaselineDimension` tenta a linha), `GetCenterlineReference(element, view)`
  (os dois usam os privados `FindCenterlineCurve` e `QualifyReference`)
  (receita do pyRevit de 2026-08-20: `IncludeNonVisibleObjects`, correção do `:INSTANCE:` e checagem de que a
  referência aponta para o próprio elemento), `GetLadders` (mesmo grid + mesmo lado = uma escada; ignora o que
  está a menos de `minDistanceFeet` do grid) → `DimensionLadderInfo`, `CreateBaselineDimension` (grid primeiro,
  depois os tubos do mais perto ao mais longe; precisa de `Transaction` aberta).
- Ficha `DimensionLadderInfo` (`Grid`, `GridOrientation`, `IsOnPositiveSide`, `Parts`) em `Utilities/Data/`.
- **Pastas (2026-10-19):** enums em `Utilities/Enums/`, fichas/resultados em `Utilities/Data/` (inclusive o
  `WorksharingResult`). Namespace continua `CSharp_for_Revit.Utilities` em tudo; `.editorconfig` da solução desliga
  o IDE0130.
- Dados: `OrientationInView` (enum: `Unknown` primeiro = padrão, `Horizontal`, `Vertical`, `Diagonal`, `Riser`),
  `FabricationCid` (enum de CIDs CADmep; 2160 e 2522 repetidos de propósito, como na tabela da Autodesk),
  `FabPartInfo` (ficha do tubo na vista: `Part`, `Cid`, `StartPointInView`, `EndPointInView`, `MidPointInView`,
  `Orientation`, `DiameterFeet` via `FABRICATION_PART_DIAMETER_IN`, `LengthFeet` via `CenterlineLength`).
  *Compilado e testado pelo Bruno em 2026-10-19.*
- `Element_Utilities`: `GetElementsOfCategory`, `IsNullOrInvalid`, `IsValid`, `IntegerValue` (`#if`),
  `GetStringParameterValue(element, BuiltInParameter, ...)`, `GetDoubleParameterValue(element, BuiltInParameter, ...)`
  (*testado em 2026-10-19*), `IsEditable`.
- `Excel_Utilities`: `GetWorkbook`, `GetWorkSheet`, `VerifyExcelFile`, `ReadWorksheet`.
- `Export_Utilities`: `DefaultPdfExportOptions`, `ExportSheetToPdf`.
- `FamilySymbol_Utilities`: `GetTitleBlockTypes`, `FamilySymbolKey`, `SelectTitleBlockTypes`.
- `File_Utilities`: `SetFormIcon`, `IsFileReadable`, `OpenDirectory`.
- `Form_Utilities`: `ProgressDelay`.
- `Revision_Utilities`: `RevisionKey`, `GetRevisions`, `SelectRevision`.
- `Ribbon_Utilities`: `AddRibbonTab`, `AddRibbonPanelToTab`, `GetRibbonPanelByName`, `NewPushButtonData`,
  `AddPulldownButtonData`, `AddPushButtonToPanel`, `AddPulldownButtonToPanel`, `AddSplitButtonToPanel`,
  `AddPushButtonToPulldown`, `CommandToBaseName`, `LookupTooltip`, `GetIcon`.
- `Selection_Utilities`: filtros `ISF_ByCategory`, `ISF_HiddenInView`.
- `Sheet_Utilities`: `GetProjectSheets`, `SheetKey`, `SheetExportKey`, `SelectSheets`, `AddRevision`.
- `UIDocument_Utilities`: `GetSelectedElements`, `SelectWithFilter`, `SelectElementIds`, `SelectElements`,
  `SelectElementId`, `SelectElement` (os quatro `Select...` são independentes de propósito).
- `Workset_Utilities`: `GetWorksets`, `SelectWorksets`.
- `Workshare_Utilities`: `ProcessElements` → `WorksharingResult`.

## Decisões já tomadas

- Sem genéricos em formulários (aula 22 não portada; designer não abre no .NET 8).
- Aula 19 (filtro do `BaseListView`) não portada — a caixa de texto do filtro existe mas não faz nada.
- Export de PDF sem barra de progresso; DWG não implementado.
- Por enquanto manter o padrão atual (Utilities estáticas, WinForms). WPF/MVVM entra quando o curso
  "Revit + WPF" for portado — definir a convenção de pastas com o Bruno nesse momento.

## Pendências (não corrigir sem o Bruno pedir)

**Afetam o funcionamento**
1. **`SyncTimer` não está registrado**: faltam `SyncTimer.Register(uiCtrlApp.ControlledApplication);` no
   `OnStartup` e `SyncTimer.DeRegister(uiCtrlApp.ControlledApplication);` no `OnShutdown`. Sem isso, o
   contador de sync nunca aparece.
2. Ícones de 16 px não são usados: `NewPushButtonData`/`AddPulldownButtonData` carregam o de 32 em `Image` e
   `LargeImage` (no curso, aula 10, o de 16 vai no `Image` e o de 32 no `LargeImage`).
3. `Sheet_Utilities.GetProjectSheets` tem dois overloads sobrepostos (uma chamada com só `doc` seria ambígua).
4. `Revision_Utilities.RevisionKey(includeId: true)` não fecha o `]`.
5. Significado de `Result` misturado: `Custom.Error` devolve `Cancelled`; `Sheet_Utilities.AddRevision` usa
   `Failed` para "a folha já tinha a revisão".

**Consistência (estilo e documentação)**
6. Código antigo ainda com `!` (`Element_Utilities.IsEditable`, `Workshare_Utilities`, `FormResult`, formulários),
   `== null` (`Revision_Utilities`, `Sheet_Utilities`, `FamilySymbol_Utilities`) e `if (multiSelect)` sem `== true`.
7. `Ribbon_Utilities` e `Sheet_Utilities` não são `static class` (`Sheet_Utilities` é `internal`); `SyncTimer`
   é uma classe comum só com membros estáticos.
8. Sem `/// <summary>`: partes do `Ribbon_Utilities` (`GetRibbonPanelByName`, `CommandToBaseName`,
   `LookupTooltip`, `GetIcon`), `Revision_Utilities` (`RevisionKey`, `GetRevisions`), `File_Utilities.SetFormIcon`,
   `Globals`, `Application`, `FormResult`, `WorksharingResult`, `Selection_Utilities` e os `Execute` dos comandos.
9. `RevisionKey` é o único extension method (`this`) do projeto; o padrão é método estático sem `this`.
10. Nomes fora do padrão: `Tab`/`Panel`/`Button` (variáveis locais em PascalCase no `Application`),
    `ClassName`/`BaseName` no `Ribbon_Utilities`, `GetWorkSheet` x `GetWorkbook`.
11. O bloco "título padrão no plural/singular" está repetido 6 vezes (Custom ×3, FamilySymbol, Sheet, Workset).
12. `Export1_Button`: transação numa operação que não altera o modelo, e nome sem o prefixo "C# for Revit:".
13. `Testing_Button`: availability `Project` mas não usa o documento; caminho de teste fixo.
14. Comentários em português em `Globals` e `Ribbon_Utilities`; erros de digitação ("objct", "Adling").

**Sobras**
15. `ViewModels/`, `Views/` (modelo do template, não usados), `Models/` vazio, `.github/copilot-instructions.md`
    com regras de Azure sem relação.
16. Ícones e tooltips `Stack1/2/3` sem uso; a classe tipada `Tooltips` não é usada.
17. `using` sem uso (`System.Reflection` no Application, `System.Net.NetworkInformation` no Globals,
    `System.Windows` e `System.Windows.Navigation` no Custom, `System.IO` no Ribbon_Utilities) e `using` do modelo
    do VS que repetem os globais.
18. `.csproj`: `None Remove` de Icons16 duplicado e ausente para Icons32.
