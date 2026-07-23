# Foundation review 001

## Alcance

Revisión arquitectónica y técnica adversarial del diff completo
`dev...feature/001-foundation`, sin modificar la implementación. Se contrastaron
`AGENTS.md`, `PROJECT_BRIEF.md`, `PROMPT_001_REPOSITORY_FOUNDATION.md`, el
handoff, los doce ADR, los documentos de arquitectura y seguridad, los contratos,
la implementación, las pruebas, la infraestructura local y CI.

La revisión cubrió explícitamente:

- compilación y pruebas de .NET, TypeScript y Python;
- OpenAPI, JSON Schemas, ejemplos y comportamiento de la API en ejecución;
- aislamiento por tenant, ocultación de existencia e idempotencia;
- límites de contexto, privacidad, SDK y neutralidad de dominio;
- separación entre adaptadores de desarrollo y persistencia;
- PostgreSQL, Redis, health checks, migración y Compose desde un clon limpio;
- reproducibilidad entre C# y Python;
- observabilidad, secretos, dependencias y versiones;
- correspondencia entre README, handoff, comandos locales y GitHub Actions.

Solo este informe fue creado en el repositorio. No se corrigió ningún hallazgo.

## Estado del repositorio revisado

- Rama revisada: `feature/001-foundation`.
- HEAD de implementación revisado: `d6119b926dc0f8c960ff4a9425f72abe38e8a8b6`.
- Base de comparación: `dev` en
  `cc586fadad4bc6c8c9a30a268682f0404b9e8855`.
- `main`, `origin/main`, `dev` y `origin/dev` permanecieron en
  `cc586fadad4bc6c8c9a30a268682f0404b9e8855`.
- `origin/feature/001-foundation` estaba en `d6119b9` después de `fetch` y
  `pull`.
- El árbol de trabajo estaba limpio antes de crear este informe.
- Diff verificado: 198 archivos, 14.353 inserciones, sin eliminaciones.
- Remoto: `https://github.com/Blaister9/ADEX.git`.
- Toolchains observados: .NET SDK `10.0.301` con runtimes
  `Microsoft.NETCore.App`/`Microsoft.AspNetCore.App` `10.0.9`, Node `22.22.3`,
  pnpm `11.6.0`, Python `3.12.6`, Docker `28.0.4`, Compose `2.34.0`.

Se creó además un clon temporal limpio en
`%TEMP%\adex-review-clean-20260723-0312`, exactamente en `d6119b9`, para
comprobar instalación, build, pruebas y arranque de infraestructura sin depender
de artefactos del árbol de trabajo. El clon temporal quedó limpio según Git.

## Commits revisados

Rango: `cc586fa..d6119b9`.

| Commit | Asunto |
| --- | --- |
| `fe0d994` | `chore: pin toolchains and add repository-wide governance files` |
| `25eba1f` | `docs: add architecture baseline, ADRs, threat model and roadmap` |
| `2ccb41a` | `feat: add API contracts, browser SDK and dashboard shell` |
| `02f2000` | `feat(api): add the .NET modular monolith with unit, contract and integration tests` |
| `1640d5c` | `feat(simulation): add the offline simulator and the cross-language seed contract` |
| `13642a3` | `feat(infra): add local Docker infrastructure and the bootstrap migration` |
| `422fddc` | `ci: add workflows, secret scanning, domain-neutrality check and PR template` |
| `d6119b9` | `docs: add the task 001 handoff and allowlist development tenant keys` |

## Comandos ejecutados

Los tiempos son de reloj aproximados en esta máquina. `Pass` significa código de
salida cero y salida inspeccionada; `Fail esperado` significa que se comprobó una
protección que debía impedir el arranque.

### Git y estado

| Comando | Resultado | Duración |
| --- | --- | ---: |
| `git status --short --branch` | limpio, `feature/001-foundation` | < 1 s |
| `git remote -v` | `origin` fetch/push correcto | < 1 s |
| `git branch -vv` | `main`/`dev` en `cc586fa`; feature en `d6119b9` | < 1 s |
| `git log --oneline --decorate -15` | rango esperado | < 1 s |
| `git fetch origin` | Pass | ~2 s |
| `git checkout feature/001-foundation` | ya estaba activa | < 1 s |
| `git pull origin feature/001-foundation` | ya actualizada | ~2 s |
| `git diff --stat dev...feature/001-foundation` | 198 archivos, +14.353 | < 1 s |
| `git diff --name-status dev...feature/001-foundation` | solo adiciones | < 1 s |

### .NET

| Comando | Resultado | Duración |
| --- | --- | ---: |
| `dotnet build Adex.slnx -c Release` | Pass, 0 warnings, 0 errores | 6,38 s |
| `dotnet --list-sdks; dotnet --list-runtimes` | SDK 10.0.301; runtime ASP.NET/.NET 10.0.9 | < 1 s |
| `dotnet test tests/unit/Adex.UnitTests -c Release` | Pass, 71/71 | 6,15 s |
| `dotnet test tests/contract/Adex.ContractTests -c Release` | Pass, 34/34 | 6,27 s |
| `dotnet test tests/integration/Adex.IntegrationTests -c Release` | Pass, 5/10; 5 skip explícitos | 17,17 s |
| `$env:ADEX_INTEGRATION=1; dotnet test tests/integration/Adex.IntegrationTests -c Release` | Pass, 10/10 con contenedores | 16,44 s |
| `dotnet test Adex.slnx -c Release` desde clon limpio | Pass, 110 pass + 5 skip sin opt-in | 18,02 s |
| arranque Production con provider `InMemory` | Fail esperado: excepción explícita de adaptador solo-desarrollo | 3,29 s |

### TypeScript, contratos y neutralidad

| Comando | Resultado | Duración |
| --- | --- | ---: |
| `pnpm install` | Pass | 0,69 s |
| `pnpm run lint` | Pass | 6,09 s |
| `pnpm run format` | Pass | 1,55 s |
| `pnpm run typecheck` | Pass | 5,18 s |
| `pnpm run build` | Pass; dashboard 195,46 kB y SDK compilado | 5,00 s |
| `pnpm run test` | Pass, 66/66 | 2,14 s |
| `pnpm run contracts:validate` | Pass, 36/36 | 1,90 s |
| `pnpm run check:domain-neutrality` | Pass | 0,76 s |
| `pnpm install --frozen-lockfile` desde clon limpio | Pass, 220 paquetes | 7,85 s |
| `pnpm run verify` desde clon limpio | Pass | 25,44 s |
| `pnpm run format` desde clon limpio | Pass | 1,58 s |

### Python

Los comandos con el Python global reprodujeron un bloqueo ambiental
(`No module named ruff` y `No module named adex_simulator`). Se repitieron con
el `.venv` documentado y también desde un `.venv` nuevo en el clon limpio.

| Comando | Resultado | Duración |
| --- | --- | ---: |
| `.venv\Scripts\python -m ruff check .` | Pass | 0,19 s |
| `.venv\Scripts\python -m ruff format --check .` | Pass, 10 archivos | 0,21 s |
| `.venv\Scripts\python -m pytest` | Pass, 28/28 | 0,75 s |
| `.venv\Scripts\python -m adex_simulator --scenario services-clear-winner --rounds 5000` | Pass; salida idéntica al handoff | 0,16 s |
| `python -m venv .venv` en clon limpio | Pass | 6,48 s |
| `.venv\Scripts\python -m pip install -e ".[dev]"` en clon limpio | Pass | 11,02 s |
| lint, formato, pytest y simulador en clon limpio | Pass | 1,47 s total |

El simulador reprodujo: 222 conversiones sintéticas, tasa `0.0444`, share del
mejor `0.3140` y regret `135.01`, con el aviso de datos sintéticos visible.

### Infraestructura, seguridad y CI

| Comando | Resultado | Duración |
| --- | --- | ---: |
| `docker compose config --quiet` | Pass | 0,22 s |
| `docker compose up -d --wait` | Pass; PostgreSQL y Redis healthy | 0,73 s con imágenes existentes |
| `psql`: `\dn`, ledger y `show timezone` | Pass; schema `adex`, `0001_bootstrap`, UTC | 0,25 s |
| `redis-cli ping` | Pass, `PONG` | 0,23 s |
| reset documentado `docker compose down -v`, seguido de Compose desde clon limpio | Pass; volumen nuevo y migración aplicada | 6,17 s |
| decisión y readiness con Redis detenido | Pass; decisión 200, readiness 200/degraded | ~1 s más espera del probe |
| `docker run ... gitleaks detect ...` | Pass; 9 commits, 552,41 kB, sin leaks | 2,62 s |
| parse de `.github/workflows/ci.yml` con PyYAML | Pass; 6 jobs | 0,10 s |
| `dotnet list Adex.slnx package --vulnerable --include-transitive` | Pass; sin vulnerabilidades conocidas | 8,64 s |
| `pnpm audit --audit-level high` | Pass; sin vulnerabilidades conocidas | 0,95 s |
| `pip-audit` en el entorno limpio | Fail; 7 avisos en `pip 24.2` y `pytest 8.4.2` | 2,69 s |
| `gh run list/view`, run `29975730766` | completado con conclusión `failure` | ~7 s |

### Pruebas adversariales de API

Se levantó la API real en Development y se enviaron peticiones HTTP, sin un
host de pruebas alternativo.

| Caso | Resultado observado |
| --- | --- |
| dos ejemplos válidos de decisión | ambos `200` |
| ejemplo válido de evento sin decisión | `202 accepted` |
| ejemplo de evento con `decision_id` estático | `422 unknown_decision`; no es ejecutable de forma aislada |
| inválido `empty-alternatives` | `422` |
| inválido `identifying-context` | `422` por la gramática mayúscula/guion |
| inválido `unknown-field` | **`200`**, aunque el schema lo rechaza |
| campo desconocido dentro de una alternativa | **`200`** |
| contexto `{"email":"someone@example.test"}` | **`200`** |
| dos decisiones con mismo body e `Idempotency-Key` | **dos `decision_id` distintos** |
| `Origin: https://attacker.example` | **`200`** |
| placement `does.not.exist` | **`200`** |
| properties de evento de 17.143 bytes | **`202 accepted`** |
| replay de evento | `202 duplicate` y sin segunda inserción |
| evento tenant B → decisión tenant A | `422 unknown_decision`, igual a un id inexistente |
| Redis detenido | decisión `200`; readiness `200 degraded` |

## Resultados de validación

### Confirmado

- Los cuatro proyectos .NET y los tres proyectos TypeScript compilan con los
  toolchains presentes.
- Pasan 71 pruebas unitarias, 34 contractuales, 10 de integración con
  infraestructura, 66 TypeScript y 28 Python.
- Los JSON Schemas compilan, los ejemplos se validan o rechazan según su
  clasificación y el OpenAPI se desreferencia.
- La idempotencia de eventos está implementada por `(tenant, event_id)` en el
  adaptador actual: replay `202 duplicate`, mismo id permitido en otro tenant.
- Un `decision_id` de otro tenant es indistinguible de uno inexistente.
- `decision_id`, `event_id`, `subject_id` y `correlation_id` respetan sus
  gramáticas en los casos cubiertos; correlation se devuelve en respuestas y
  problemas.
- El provider en memoria y el tenant directory están nombrados como desarrollo;
  Production con `InMemory` falla al arrancar.
- PostgreSQL está diseñado como sistema de registro y seleccionar `Postgres`
  falla explícitamente porque el adapter aún no existe; no hay fallback
  silencioso.
- Redis es opcional en el camino implementado y su caída no impide decidir.
- Liveness no consulta dependencias. Readiness devuelve `200 degraded` para
  Redis caído.
- Compose funciona desde un clon y volumen nuevos, aplica la migración y usa UTC.
- No se hallaron secretos con Gitleaks, rutas absolutas de una máquina, ni PII
  real en archivos rastreados.
- El SDK no consulta user agent, canvas, fuentes, geolocalización, cookies,
  local/session storage ni otras primitivas de fingerprinting.
- El core es neutral de dominio según inspección y el chequeo automatizado.
- Python no participa en el camino online.
- Las cifras del simulador están marcadas de manera inequívoca como sintéticas.
- README y CI comparten los comandos principales; la diferencia relevante no es
  de nombres, sino de cobertura contractual descrita en FND-006.

### No confirmado o contradicho

- La API en ejecución no coincide con las restricciones de request de los JSON
  Schemas.
- La idempotencia de decisiones descrita en OpenAPI y ADR-0012 no existe.
- La allow-list de contexto no está impuesta.
- Origin, placement y rate-limit aparecen como comportamiento del contrato
  actual, pero no están implementados.
- El límite de 4 KiB de `properties` no está aplicado.
- La equivalencia C#/Python cubre la derivación de seed, no la selección de
  policy.
- La observabilidad no implementa las tres señales y atributos afirmados.
- Las versiones no están todas fijadas ni justificadas con estado de soporte
  vigente.
- GitHub Actions sí se ejecutó; el run del commit revisado terminó en failure.

## Resultado de GitHub Actions

Run inspeccionado:
`https://github.com/Blaister9/ADEX/actions/runs/29975730766`, HEAD `d6119b9`,
iniciado `2026-07-23T02:58:00Z`, finalizado `02:58:57Z`, conclusión
**failure**.

| Job | Resultado |
| --- | --- |
| `.NET build and tests` | Pass, incluyendo servicios y 10 integration tests |
| `Simulator lint and tests` | Pass |
| `Infrastructure configuration` | Pass |
| `Secret scan` | Pass |
| `TypeScript build, lint and tests` | **Fail en install** |
| `Dependency vulnerability report` | Fail tolerado (`continue-on-error`) |

Los dos fallos partieron de
`ERR_PNPM_MINIMUM_RELEASE_AGE_VIOLATION`: `happy-dom@20.11.1` había sido
publicado dentro de la ventana mínima al ejecutarse CI. El job TypeScript no
llegó a lint/build/tests; el job de dependencias no llegó a `pip-audit`. Horas
después, el mismo `pnpm install --frozen-lockfile` pasó desde un clon limpio
porque la entrada ya superó/verificó la política. Es un fallo transitorio real
del único run del commit revisado, no un fallo local reproducible al final de
esta revisión.

## Hallazgos

### FND-001 — BLOCKER — La API acepta requests que el contrato cerrado declara inválidos

**Evidencia verificable.** El schema de decisión usa
`additionalProperties: false`, y el ejemplo
`decision-request.unknown-field.json` es rechazado correctamente por Ajv. La API
real recibió ese mismo archivo y devolvió `200` con una decisión. También aceptó
un campo `weight` desconocido dentro de una alternativa. Todas las suites
existentes siguieron verdes.

**Archivo y símbolo.**

- `packages/contracts/schemas/decision-request.schema.json:5-9`
- `packages/contracts/schemas/common.schema.json:82-89`
- `src/Adex.Api/Program.cs:14-18`,
  `builder.Services.ConfigureHttpJsonOptions`
- `src/Adex.Api/Contracts/PublicContracts.cs`, DTO de request

**Reproducción.**

```text
POST /v1/decisions
X-Adex-Api-Key: pk_dev_reference_services
{"placement":"homepage.primary-cta",
 "eligible_alternatives":[{"key":"variant-a"}],
 "force_alternative":"variant-a"}
```

Resultado actual: `200`. Resultado contractual: rechazo, documentado como
`422`.

**Impacto.** El supuesto contract-first no constriñe el servidor. Typos y
campos que un integrador crea activos se ignoran silenciosamente; el ejemplo
inválido oficial es aceptado. Esto impide integrar la fundación bajo sus propios
criterios.

**Corrección mínima recomendada.** Configurar System.Text.Json para rechazar
miembros no mapeados en objetos request, convertir ese error al problem envelope
contractual y añadir pruebas contra los ejemplos inválidos, incluidos miembros
anidados.

**Criterio objetivo de resolución.** Todos los ejemplos request de
`examples/invalid` devuelven `4xx` coherente con OpenAPI, los ejemplos válidos
siguen siendo aceptados y una prueba que añada un miembro desconocido tanto al
root como a una alternativa falla antes de la corrección y pasa después.

### FND-002 — HIGH — `Idempotency-Key` de decisiones está publicado pero se ignora

**Evidencia verificable.** Dos requests idénticos con
`Idempotency-Key: same-request-001` devolvieron
`dec_01KY6F82ER1R58KTDKC0TZMF58` y
`dec_01KY6F82ET0A480X1F7VGVHBNY`. El endpoint no lee el header ni existe un
puerto/store para esta semántica.

**Archivo y símbolo.**

- `packages/contracts/openapi/adex-public-v1.yaml:52-60,207-216,278-282`
- `docs/adr/0012-idempotency-and-deduplication.md:39-44`
- `src/Adex.Api/Endpoints/DecisionEndpoints.cs:13-98`
- `src/Adex.Application/Decisions/RequestDecision.cs:48-105`

**Reproducción.** Enviar dos veces el mismo body anterior con el mismo
`Idempotency-Key` y comparar `decision_id`.

**Impacto.** Un retry de red que el contrato promete colapsar crea dos decisiones
y potencialmente dos impresiones/auditorías. El comportamiento público y la
implementación divergen en una semántica de integridad.

**Corrección mínima recomendada.** Implementar el registro tenant-scoped de
idempotency con hash del body, TTL de 24 horas y `409` en reutilización con body
distinto; o retirar la promesa del contrato antes de publicar v1 si se decide
formalmente diferirla.

**Criterio objetivo de resolución.** Mismo tenant + key + body devuelve el mismo
`decision_id`; mismo tenant + key + body distinto devuelve `409`; otro tenant
puede reutilizar la key; existen pruebas concurrentes.

### FND-003 — HIGH — La allow-list de contexto no existe y se acepta PII bajo claves bien formadas

**Evidencia verificable.** La API aceptó con `200`:

```json
{"context":{"email":"someone@example.test"}}
```

`ReadValueMap` solo verifica regex, tipo y longitud. No consulta una lista base
ni configuración del tenant. El inválido oficial usa `Visitor-Email`, que falla
por mayúscula/guion, no por ser email.

**Archivo y símbolo.**

- `src/Adex.Api/Validation/RequestValidation.cs:257-307`, `ReadValueMap`
- `packages/contracts/schemas/common.schema.json:72-81`
- `packages/contracts/src/index.ts:41-47`, `BASE_CONTEXT_KEYS`
- `docs/adr/0008-anonymous-identity-and-privacy-defaults.md:41-57`
- `docs/security/threat-model-initial.md`, amenazas T5/T7

**Reproducción.** Enviar una decisión válida agregando
`context.email`, `context.full_name` u otra clave lower_snake_case no declarada.

**Impacto.** La frontera pública permite recolectar y mantener en el audit record
datos identificables que la postura “privacy by default” afirma rechazar. Es un
riesgo serio de privacidad y cumplimiento.

**Corrección mínima recomendada.** Hasta que exista configuración tenant-scoped,
aceptar solo la lista base declarada; después, resolver el allow-list de la
placement/tenant y rechazar el resto con `unknown_context_key`. La validación de
valores sensibles no sustituye la lista.

**Criterio objetivo de resolución.** `email` y cualquier clave no configurada
devuelven `422 unknown_context_key`; las cinco claves base y claves adicionales
explícitamente configuradas se aceptan; las pruebas cubren dos tenants con
listas distintas.

### FND-004 — MEDIUM — OpenAPI describe controles y estados que la API actual no puede producir

**Evidencia verificable.**

- `Origin: https://attacker.example` devolvió `200`, no `403`.
- placement `does.not.exist` devolvió `200`, no `404`.
- `UniformRandomPolicyResolver` resuelve cualquier placement.
- No existe rate limiter ni camino `429`.
- El propio README reconoce que origin, placement config y rate limiting están
  diferidos.

**Archivo y símbolo.**

- `packages/contracts/openapi/adex-public-v1.yaml:48-91,188-197,268-292`
- `src/Adex.Infrastructure/Tenancy/ConfiguredTenantDirectory.cs:8-17`
- `src/Adex.Infrastructure/Policies/UniformRandomPolicyResolver.cs`
- `src/README.md:35-43`

**Reproducción.** Enviar una decisión válida con un `Origin` arbitrario y luego
con una placement inventada.

**Impacto.** OpenAPI se declara fuente de verdad de la API ejecutable, pero
mezcla contrato actual con objetivos de roadmap. Clientes, pruebas de seguridad
y operadores no pueden confiar en los status publicados.

**Corrección mínima recomendada.** Decidir explícitamente si el documento
describe la fundación ejecutable o el futuro MVP. Para la primera opción,
retirar/marcar como no disponible lo diferido; para la segunda, implementar los
controles antes de afirmar alineación. No basta con listar códigos sin una
prueba capaz de producirlos.

**Criterio objetivo de resolución.** Cada status específico publicado tiene una
prueba negra reproducible en la API real, o deja de figurar como comportamiento
actual; origin y placement se validan tenant-scoped.

### FND-005 — MEDIUM — No se aplica el límite contractual de 4 KiB para `properties`

**Evidencia verificable.** Un evento con 64 propiedades de 256 caracteres,
17.143 bytes serializados solo para `properties`, devolvió `202 accepted`. El
schema describe un límite total de 4 KiB, pero solo impone 64 claves y longitud
individual. Kestrel limita el body completo a 64 KiB.

**Archivo y símbolo.**

- `packages/contracts/schemas/event-request.schema.json:27-33`
- `src/Adex.Api/Validation/RequestValidation.cs:31-35,218-225,257-307`
- `src/Adex.Api/Program.cs:10-12`

**Reproducción.** Enviar 64 claves lower_snake_case, cada una con 256 caracteres.

**Impacto.** Se amplía cuatro veces la superficie de almacenamiento y abuso que
la amenaza T7 afirma controlar, y el contrato da una garantía falsa.

**Corrección mínima recomendada.** Medir bytes UTF-8 serializados de
`properties` en la frontera y rechazar más de 4.096 con error de campo, o cambiar
el contrato y threat model si el límite real elegido es otro.

**Criterio objetivo de resolución.** Payload de `properties` en el límite es
aceptado; uno de 4.097 bytes es rechazado; caracteres multibyte se cuentan por
bytes, no por UTF-16 chars.

### FND-006 — MEDIUM — Las pruebas “contract” no prueban conformidad entre schemas y servidor

**Evidencia verificable.** Ajv rechaza `unknown-field`, pero la suite .NET
contractual no publica ese ejemplo y queda verde mientras la API lo acepta.
`openapi.test.ts` verifica paths, refs y la lista de status, no ejecuta
operaciones. Las respuestas exitosas .NET se inspeccionan a mano y no se
validan contra los schemas.

**Archivo y símbolo.**

- `packages/contracts/test/schemas.test.ts:72-157`
- `packages/contracts/test/openapi.test.ts:48-122`
- `tests/contract/Adex.ContractTests/DecisionContractTests.cs`
- `tests/contract/Adex.ContractTests/EventContractTests.cs`

**Reproducción.** Ejecutar ambas suites (verdes) y luego publicar
`decision-request.unknown-field.json` (respuesta `200`).

**Impacto.** El mecanismo que ADR-0006 presenta como control de drift no detecta
drift real. Nuevos cambios pueden romper el contrato con CI verde.

**Corrección mínima recomendada.** Añadir un harness de conformidad que publique
todos los ejemplos request aplicables, valide todas las respuestas contra JSON
Schema/OpenAPI y cubra las semánticas no expresables solo por schema
(idempotency, tenant, origin, clock).

**Criterio objetivo de resolución.** Una mutación que vuelva a permitir un campo
desconocido, omita un campo response o cambie un status documentado hace fallar
la suite contractual.

### FND-007 — MEDIUM — La equivalencia C#/Python cubre seeds, no la policy duplicada

**Evidencia verificable.** Los fixtures compartidos contienen derivación de
seed. C# y Python implementan por separado `seed % eligible.Count`, propensity
y orden, pero no consumen un fixture compartido de selección. El handoff también
reconoce que la mitigación “solo cubre seed derivation”.

**Archivo y símbolo.**

- `tests/fixtures/decision-seed-vectors.json`
- `tests/unit/Adex.UnitTests/CrossLanguageSeedTests.cs`
- `simulation/adex-simulator/tests/test_seeds.py`
- `src/Adex.Domain/Policies/UniformRandomPolicy.cs`
- `simulation/adex-simulator/src/adex_simulator/policies.py:32-51`
- `docs/adr/0003-language-boundaries.md:60-66`

**Reproducción.** Cambiar localmente el índice de selección en una sola
implementación manteniendo la derivación de seed; las pruebas cross-language
siguen pasando.

**Impacto.** El simulador puede evaluar una policy distinta de la online sin que
el contrato cross-language lo detecte, debilitando reproducibilidad y evidencia
offline.

**Corrección mínima recomendada.** Añadir vectores compartidos con alternativas,
seed, selección y propensity; consumirlos desde ambos lenguajes. Extender el
contrato por policy promovida.

**Criterio objetivo de resolución.** Una mutación del módulo, orden de
alternativas o propensity en cualquiera de los lenguajes rompe al menos una
prueba compartida.

### FND-008 — MEDIUM — La línea base no está completamente fijada y su rationale de soporte está desactualizado

**Evidencia verificable.**

- `global.json` permite `rollForward: latestFeature`; `.nvmrc` fija solo `22`;
  Python fija un rango minor, no un patch.
- `pyproject.toml` usa rangos para `pytest`, `ruff` y hatchling, sin lockfile. El
  clon limpio resolvió `pytest 8.4.2`; `pip-audit` reportó un advisory para esa
  dependencia, además de advisories del `pip 24.2` incluido por venv.
- ADR-0002 llama Node 22 “active LTS”; el calendario oficial lo clasifica
  Maintenance LTS desde 2025-10-21, mientras Node 24 es Active LTS.
- ADR-0002 afirma Python 3.12 “full support”; la tabla oficial lo clasifica
  security-only.
- Se fijan paquetes ASP.NET Core `10.0.9`, mientras la política oficial lista
  `10.0.10` como patch vigente y exige mantenerse en el patch actual para
  soporte.

Fuentes oficiales consultadas:

- .NET support policy:
  `https://dotnet.microsoft.com/en-us/platform/support/policy`
- Node Release Working Group:
  `https://github.com/nodejs/Release`
- Python version status:
  `https://devguide.python.org/versions/`
- PostgreSQL versioning:
  `https://www.postgresql.org/support/versioning/`
- Redis OSS version management:
  `https://redis.io/docs/latest/operate/oss_and_stack/install/version-mgmt/`

PostgreSQL 17 y Redis 7.4 sí están soportados. Node 22 y Python 3.12 también
siguen soportados; el defecto es de fase/rationale, horizonte y
reproducibilidad, no que sean EOL.

**Archivo y símbolo.**

- `docs/adr/0002-toolchain-and-runtime-baseline.md:17-32`
- `global.json:2-6`
- `.nvmrc`
- `package.json:7-11`
- `simulation/adex-simulator/pyproject.toml:1-16`
- `Directory.Packages.props:9-29`

**Reproducción.** Crear un venv limpio, instalar `.[dev]`, listar versiones y
ejecutar `pip-audit`; comparar la tabla ADR con las fuentes oficiales.

**Impacto.** Dos clones hechos en fechas distintas no tienen garantizado el
mismo entorno Python; CI puede incorporar vulnerabilidades o cambios sin diff.
Las decisiones de soporte no tienen el fundamento vigente exigido.

**Corrección mínima recomendada.** Elegir y documentar si se desea patch exacto
o actualización automática controlada por lock/renovación; añadir lock/hashes
para Python; actualizar dependencias vulnerables; corregir las fases de soporte
y revisar el patch .NET. Node 22 puede conservarse si se justifica
deliberadamente como Maintenance LTS.

**Criterio objetivo de resolución.** Instalaciones limpias resuelven las mismas
versiones; el audit no reporta vulnerabilidades en dependencias del proyecto; la
tabla ADR coincide con las fuentes oficiales y declara el proceso de upgrades.

### FND-009 — MEDIUM — Observabilidad afirma tres señales y degradación medible que no están cableadas

**Evidencia verificable.** `AddAdexTelemetry` configura tracing y metrics, no
logging OpenTelemetry. No hay middleware que emita el log estructurado por
request con correlation, tenant, route, status y duration descrito en ADR-0011.
El contador `AdexTelemetry.CacheDegraded` se declara pero no tiene ninguna
llamada `Add`; una caída real de Redis solo produce un warning del probe.

**Archivo y símbolo.**

- `docs/adr/0011-observability-baseline.md:15-38`
- `src/Adex.Api/Telemetry/TelemetryServiceCollectionExtensions.cs:17-58`
- `src/Adex.Api/Telemetry/AdexTelemetry.cs:37-40`
- `src/Adex.Infrastructure/Caching/RedisDependencyProbe.cs:45-56`

**Reproducción.** Buscar usos de `CacheDegraded` (solo declaración), detener
Redis y observar que no se incrementa el instrumento; inspeccionar el builder de
OpenTelemetry y comprobar ausencia de logs.

**Impacto.** Operaciones y seguridad no reciben las señales afirmadas; un
dashboard basado en `adex.cache.degraded` mostraría cero durante una caída.

**Corrección mínima recomendada.** Implementar export de logs compatible con
OTel o reducir la afirmación; añadir logging scope/middleware con atributos
acotados; incrementar la métrica en el punto real de bypass (no solo por polling
de health).

**Criterio objetivo de resolución.** Una prueba con exporter/reader en memoria
observa el log estructurado completo y al menos un incremento de
`adex.cache.degraded` al ejecutar un camino de aplicación con Redis no
disponible, sin cardinalidad prohibida.

### FND-010 — LOW — Readiness da una explicación falsa bajo el provider en memoria

**Evidencia verificable.** Con Redis detenido, readiness devolvió correctamente
`200 degraded`, pero el detail fue “ADEX continues to serve from PostgreSQL”.
En la misma respuesta PostgreSQL figuró `required:false`, y las decisiones
continuaron desde `InMemoryDecisionStore`, no desde PostgreSQL.

**Archivo y símbolo.**

- `src/Adex.Infrastructure/Caching/RedisDependencyProbe.cs:50-56`
- `src/Adex.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs:101-123`

**Reproducción.** Ejecutar en Development con provider `InMemory`, detener Redis
y consultar `/health/ready`.

**Impacto.** El status/HTTP son correctos, pero el operador recibe un diagnóstico
falso sobre el sistema de registro usado.

**Corrección mínima recomendada.** Usar texto neutral (“continues without cache
acceleration”) o construir el detalle según el provider real.

**Criterio objetivo de resolución.** Ninguna respuesta de health afirma uso de
PostgreSQL cuando `Persistence:Provider=InMemory`.

### FND-011 — NOTE — El único run CI del commit revisado falló por una política temporal de supply chain

**Evidencia verificable.** Run `29975730766` terminó failure por
`ERR_PNPM_MINIMUM_RELEASE_AGE_VIOLATION` para `happy-dom@20.11.1`; más tarde el
mismo frozen install pasó en un clon limpio.

**Archivo y símbolo.**

- `.github/workflows/ci.yml:89-111,192-229`
- `pnpm-lock.yaml`, entrada `happy-dom@20.11.1`

**Reproducción.** Ver logs del run. La reproducción local posterior ya no falla
porque la condición depende de la edad de publicación.

**Impacto.** El commit revisado no tiene un run completamente verde y los
resultados TypeScript no fueron ejecutados en ese run, aunque la matriz local y
el clon limpio sí pasaron.

**Corrección mínima recomendada.** Reejecutar CI cuando la política lo permita y
registrar el resultado. No relajar la política solo para obtener verde.

**Criterio objetivo de resolución.** Un run completo sobre el mismo contenido de
implementación finaliza con todos los jobs requeridos verdes; los jobs
informativos pueden reportar advisories sin ocultar pasos no ejecutados.

## Afirmaciones del handoff confirmadas

- Branch y rango de ocho commits.
- Build .NET sin warnings ni errores.
- 71 unit, 34 contract, 5 pass + 5 skip sin opt-in y 10/10 integration con
  contenedores.
- 66 pruebas TypeScript y 28 Python.
- Cifras exactas del simulador de 5.000 rondas.
- Compose healthy, migración aplicada, PostgreSQL UTC y Redis `PONG`.
- Gitleaks sin secretos.
- Problema de JSON malformado corregido a `400`.
- Redis degradable para el camino actualmente implementado.
- Persistencia exclusivamente en memoria, correctamente rotulada y bloqueada
  fuera de Development.
- Ausencia de RLS/entity schema, origin allow-list, rate limit, configuración de
  placement, dashboard real y policies adaptativas.
- Neutralidad de dominio y ausencia de fingerprinting en el SDK.
- Objetivos de latencia no medidos y no presentados como resultados.

## Afirmaciones no confirmadas o incorrectas

- “OpenAPI/JSON Schema y running API coinciden”: incorrecto por FND-001,
  FND-002, FND-004 y FND-005.
- “Los invalid examples son rechazados”: incorrecto en la API real para
  `decision-request.unknown-field.json`; solo es cierto en Ajv.
- “Context allow-list defined and validated”: incorrecto para claves
  lower_snake_case no declaradas.
- “Idempotency de decisiones” de ADR/OpenAPI: no implementada.
- “Contract tests assert the running API matches the same semantics”: demasiado
  amplio; FND-006 demuestra un contraejemplo.
- “OpenTelemetry for all three signals” y `adex.cache.degraded`: no confirmado
  por FND-009.
- “Cross-language fixtures” como mitigación de policy: solo seed, no selección.
- Node 22 como Active LTS y Python 3.12 en full support: incorrectos a la fecha
  de revisión.
- “197 files, +14.080”: el diff real observado es 198 archivos, +14.353.
- “GitHub Actions workflow has never run”: dejó de ser cierto después del push;
  el run existe y terminó failure.

## Riesgos residuales

- No existe persistencia productiva ni RLS. Está claramente diferido, pero nada
  que dependa de durability o aislamiento en base de datos puede considerarse
  demostrado.
- No hay e2e vertical, attribution/reward, cierre de ventanas ni analytics. Son
  deudas declaradas, no se clasifican como defectos nuevos de esta fundación.
- Las API keys de Development carecen deliberadamente de hashing, origin,
  rotation y rate limits; el fail-fast fuera de Development reduce el riesgo de
  confusión, pero el OpenAPI debe dejar de presentarlo como activo.
- No se midió latencia, backup/restore, carga, rate limit, failover de PostgreSQL
  ni eliminación por subject. La documentación los trata como objetivos.
- Los tags de imágenes usan major/minor y no digest; el riesgo está declarado.
- El job de vulnerabilidades es informativo y `continue-on-error`; además, el
  run revisado no llegó a `pip-audit`.

Resumen de hallazgos:

| Severidad | Cantidad |
| --- | ---: |
| BLOCKER | 1 |
| HIGH | 2 |
| MEDIUM | 6 |
| LOW | 1 |
| NOTE | 1 |

## Recomendación final

REJECT UNTIL FIXED: no debe integrarse hasta corregir los hallazgos indicados.
