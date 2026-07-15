# ROADMAP — Pelican Town System (PTS)

> Sistema de gestão da cidade de Pelican Town. Projeto-vitrine de portfólio, construído para demonstrar domínio de C#/.NET em cenário de complexidade real: múltiplos domínios de negócio, RBAC, mensageria, microsserviços e boas práticas de arquitetura.
>
> Onde existe uma estrutura real do jogo, o módulo usa o nome dela (ex: `JojaMart`, `Clínica do Doutor Harvey`) em vez de um nome genérico — reforça a identidade do projeto e facilita a narrativa em entrevista.

## Princípio geral

O projeto será construído em **duas grandes etapas**:

1. **Monólito Modular** — todos os módulos vivem no mesmo processo, mas com fronteiras de domínio bem definidas (cada módulo = seu próprio `Domain`/`Application`/`Infrastructure`, sem referência direta entre módulos, só comunicação via interfaces/eventos internos). Um schema PostgreSQL por módulo desde o início.
2. **Decomposição em Microsserviços** — depois que o domínio estiver maduro e testado, os módulos com fronteiras mais fortes são extraídos para serviços independentes, comunicando-se via mensageria (RabbitMQ/MassTransit) e um API Gateway.

---

## Fase 0 — Fundação e arquitetura

- [ ] Definir bounded contexts de todos os módulos (lista completa na tabela ao final)
- [ ] Modelo de "Cidadão" como identidade compartilhada (shared kernel mínimo)
- [ ] Estrutura de solution: `src/Modules/<NomeDoModulo>/{Domain,Application,Infrastructure,Api}` + `src/Shared/{Kernel,Contracts}`
- [ ] Um schema PostgreSQL por módulo desde o início
- [ ] `docker-compose.yml` base: Postgres, RabbitMQ, Redis, Seq (logging estruturado)
- [ ] Shared Kernel: Value Objects comuns (`Money`, `Address`, `Cpf`, `Phone`), `BaseEntity`, `IDomainEvent`, `Result<T>` / Either para erros de domínio
- [ ] Configurar Serilog + Seq com correlation-id propagado entre módulos
- [ ] Swagger/OpenAPI com Bearer configurado globalmente
- [ ] Planejar estratégia de cloud (Azure) e IaC (Terraform) — define direção desde o início, implementa na Fase 33
- [ ] Pipeline de CI com GitHub Actions (build + testes no PR)
- [ ] Script `./scripts/scaffold-module.sh` — gera estrutura de 4 projetos (Domain, Application, Infrastructure, Api) com namespaces, DI registration e projeto de testes
- [ ] Script `./scripts/setup.sh` — sobe containers, roda migrations de todos os módulos, aplica seed data, deixa API pronta em um comando
- [ ] Sistema de seed data: `app.SeedAsync()` no host que popula cidadãos famosos (Prefeito Lewis, Doutor Harvey, Clint, Robin, Gus, Willy, Mago Rasmodius, Krobus, Marnie, Gunther), suas roles, e dados de demonstração por módulo
- [ ] README com a visão do produto e o mapa de módulos

---

## Fase 1 — Núcleo: Identity & Access

### Tópicos
- [ ] Cadastro de Cidadão, autenticação (JWT + refresh token com token rotation)
- [ ] Password hashing, lockout por tentativas, 2FA via TOTP (opcional por cidadão)
- [ ] Refresh token armazenado em Redis com TTL
- [ ] Middleware de validação de token revogado
- [ ] RBAC: Papéis e Permissões desacoplados — um cidadão acumula papéis de módulos diferentes (ex: Médico na Clínica **e** Fazendeiro)
- [ ] Policy-based authorization do ASP.NET Core
- [ ] Rate limiting nos endpoints de login/register
- [ ] Endpoint que retorna papéis/permissões do cidadão autenticado, consumido pelos outros módulos
- [ ] Auditoria de autenticação (login, logout, falha, refresh)
- [ ] Testes unitários (xUnit + Moq) e de integração (WebApplicationFactory + Testcontainers)

### Projeto prático
**`pts-identity`** — identidade, autenticação e RBAC.

---

## Fase 2 — Infraestrutura compartilhada da API

- [ ] Middleware global de exceções (Problem Details — RFC 7807)
- [ ] Rate limiting por cidadão e por IP (ASP.NET Core built-in)
- [ ] Health checks da aplicação e dos serviços de infra (Postgres, Redis, RabbitMQ)
- [ ] Correlation-id propagado via header e incluído em toda resposta/erro
- [ ] CORS policy unificada

> O API Gateway (YARP) será introduzido na Fase 25 junto com a decomposição em microsserviços. Durante o monólito, não há necessidade de proxy reverso.

---

## Fase 3 — Prefeitura (Town Hall)

Módulo mais simples — valida o padrão de módulo com baixo risco.

### Tópicos
- [ ] Cadastro de imóveis/lotes e proprietários (referência a Cidadão)
- [ ] Emissão de licenças/alvarás (ex: alvará da JojaMart, alvará de obra da Carpintaria)
- [ ] Registro e cobrança de impostos municipais (IPTU, ISS)
- [ ] Taxas configuráveis por ano fiscal
- [ ] Workflow de aprovação (solicitado → em análise → aprovado/rejeitado)
- [ ] Histórico de pagamentos e inadimplência
- [ ] Background job: cálculo mensal de impostos recorrentes (Quartz.NET)
- [ ] Testes unitários + integração

### Projeto prático
**`pts-town-hall`** — imóveis, licenças e impostos, sob gestão do Prefeito Lewis.

---

## Fase 4 — Ferraria do Clint

Domínio pequeno e isolado, ideal para introduzir **fila de serviço com SLA** sem repetir o padrão de nenhum outro módulo.

### Tópicos
- [ ] Cadastro de pedidos de melhoria/reparo de ferramentas e equipamentos
- [ ] Máquina de estados do pedido: recebido → em processamento → pronto → retirado
- [ ] Prazo de entrega calculado por tipo de serviço (upgrade de ferramenta demora "dias", como no jogo)
- [ ] Job em background (Quartz.NET) que avança pedidos "em processamento" até "pronto" quando o prazo vence
- [ ] Notificação ao cidadão quando o pedido fica pronto (via evento de domínio interno)
- [ ] Testes unitários + integração (incluindo simulação de avanço do Quartz job)

### Projeto prático
**`pts-ferraria`** — fila de melhorias/reparos com SLA e processamento assíncrono via background job.

---

## Fase 5 — Carpintaria do Robin

Primeiro módulo desenhado desde o início para depender de **outro módulo** — bom lugar pra introduzir integração síncrona entre módulos (que depois, na Fase 23, vira Saga assíncrona).

### Tópicos
- [ ] Cadastro de obras/reformas de edifícios da cidade
- [ ] Toda obra exige alvará aprovado pela Prefeitura antes de iniciar (chamada síncrona entre módulos nesta fase)
- [ ] Orçamento e cronograma de obra
- [ ] Estados: orçado → aguardando alvará → em construção → concluída
- [ ] Testes unitários + integração (com mock da Prefeitura e com Prefeitura real)

### Projeto prático
**`pts-carpintaria`** — obras da cidade, integradas ao fluxo de alvará da Prefeitura.

---

## Fase 6 — Clínica do Doutor Harvey

### Tópicos
- [ ] Papéis: Médico, Recepcionista, Paciente (Cidadãos com papel atribuído)
- [ ] Prontuário eletrônico (histórico, diagnósticos, alergias, prescrições)
- [ ] SOAP notes — Subjective, Objective, Assessment, Plan
- [ ] Agendamento de consultas com concorrência otimista (evitar overbooking no slot)
- [ ] Regras de negócio: limite de consultas/dia por médico, conflito de horário
- [ ] Regras de acesso: paciente só vê o próprio prontuário; médico só vê quem atendeu; recepcionista não vê diagnóstico
- [ ] Histórico paginado e filtrável (por período, médico, especialidade)
- [ ] Testes unitários + integração (cenários de concorrência no agendamento)

### Projeto prático
**`pts-clinica-harvey`** — médicos, recepcionistas, pacientes, prontuário e agendamento.

---

## Fase 7 — Rancho da Marnie

Reaproveita o **padrão** de agendamento da Clínica em outro domínio, sem acoplar código entre os dois módulos — bom argumento de "reuso de conceito, não de classe".

### Tópicos
- [ ] Cadastro de animais por cidadão (fazendeiro)
- [ ] Agenda de cuidados veterinários e vacinação (mesma lógica de slot/concorrência da Clínica, implementada de forma independente)
- [ ] Produção animal (leite, lã, ovos) como insumo para outros módulos
- [ ] Testes unitários + integração

### Projeto prático
**`pts-rancho-marnie`** — cadastro de animais e agenda veterinária.

---

## Fase 8 — JojaMart *(antigo "Supermercado")*

No jogo, a JojaMart é a rede corporativa que compete com o comércio local — e só é "derrotada" quando o Centro Comunitário é restaurado. Vamos manter essa tensão temática: a JojaMart é o módulo dominante de varejo até o Centro Comunitário (Fase 22) entrar em cena.

### Tópicos
- [ ] Catálogo de produtos (categorias, subcategorias, código de barras)
- [ ] Controle de estoque (entrada, saída, reserva na venda, baixa por validade, inventário)
- [ ] Fornecedores e pedidos de compra
- [ ] Ponto de venda (compra, desconto, troco, cupom fiscal simplificado)
- [ ] Aplicação de impostos (reaproveitando o cadastro de impostos da Prefeitura)
- [ ] Controle de concorrência no estoque (optimistic concurrency + retry)
- [ ] Carrinho de compras com caching em Redis
- [ ] Relatórios de vendas por período, produto, categoria
- [ ] Conciliação de estoque (esperado vs real)
- [ ] Eventos de domínio: `ProductSold`, `StockLow` (alerta reposição)
- [ ] Testes unitários + integração (cenários de concorrência de venda)

### Projeto prático
**`pts-jojamart`** — estoque, vendas, cupons e impostos.

> Extensão opcional futura: um segundo módulo `pts-general-store` (Loja do Pierre) como concorrente local de menor porte — só valeria a pena se o objetivo for demonstrar *multi-tenancy* com o mesmo código-base.

---

## Fase 9 — Escola de Pelican Town

A "schoolhouse" existe no jogo como estrutura real, mesmo sem função formal — aqui ela ganha um papel completo.

### Tópicos
- [ ] Cadastro de alunos (crianças vinculadas a um responsável, sem login próprio)
- [ ] Papéis: Professor, Responsável, Aluno
- [ ] Matrícula, turmas, grade curricular e disciplinas
- [ ] Lançamento de notas e frequência
- [ ] Regras de domínio: nota mínima para aprovação, limite de faltas
- [ ] Boletim em PDF (QuestPDF)
- [ ] Comunicados aos responsáveis
- [ ] Calendário letivo e eventos (reunião de pais, feriados, conselho de classe)
- [ ] Testes unitários + integração

### Projeto prático
**`pts-escola`** — matrícula, turmas, notas e boletins.

---

## Fase 10 — Peixaria do Willy

### Tópicos
- [ ] Catálogo de pescado com preços variáveis por sazonalidade
- [ ] Registro de vendas (reaproveita conceitos da JojaMart, sem acoplar código)
- [ ] Fornecedores (pescadores como Cidadãos fornecedores)
- [ ] Evento: `PeixeRaroCapturado` (consumido pelo Museu na Fase 19 e pelo Esgotos na Fase 12)
- [ ] Testes unitários + integração

### Projeto prático
**`pts-peixaria-willy`** — catálogo sazonal e vendas.

---

## Fase 11 — Stardrop Saloon (Saloon do Gus)

O ponto de encontro dos NPCs às sextas-feiras. Primeiro módulo do roadmap a introduzir **comandas e pedidos de cozinha**.

### Tópicos
- [ ] Cardápio com itens, preços e disponibilidade por dia
- [ ] Comandas por mesa/cliente (tab system: abre, adiciona itens, fecha)
- [ ] Papéis: Garçom, Cozinheiro, Cliente
- [ ] Fluxo da cozinha: pedido recebido → em preparo → pronto para servir
- [ ] Mesas e capacidade do salão
- [ ] Evento semanal "Friday Night" com cardápio especial e preços promocionais
- [ ] Fornecedores de insumos (integração com Fazenda, Rancho, Peixaria)
- [ ] Testes unitários + integração

### Projeto prático
**`pts-salao-gus`** — cardápio, comandas, fluxo de cozinha e evento semanal.

---

## Fase 12 — Esgotos / Krobus

Domínio pequeno com temática única: o comércio subterrâneo da cidade. Introduz **reputação como mecânica de desbloqueio de conteúdo**.

### Tópicos
- [ ] Catálogo de itens raros e exóticos (alguns só desbloqueiam com reputação)
- [ ] Sistema de reputação com o povo das sombras (Krobus) — quanto maior, mais itens disponíveis
- [ ] Papéis: Krobus (vendedor único), Cliente
- [ ] Consome eventos de Peixaria (peixe raro), Guilda (drop de monstro) e Museu (artefato doado) para abastecer o catálogo de itens raros
- [ ] Testes unitários + integração

### Projeto prático
**`pts-esgotos-krobus`** — comércio de itens raros, reputação com o povo das sombras.

---

## Fase 13 — Spa / Casa de Banho

Controle de capacidade por slot de tempo — padrão diferente do agendamento 1:1 da Clínica. A Estação de Trem fica ao lado e funciona como ponto de chegada/partida.

### Tópicos
- [ ] Agendamento de horários com capacidade máxima por faixa (ex: 20 pessoas das 9h-11h)
- [ ] Tipos de serviço: banho termal, massagem, sauna
- [ ] Papéis: Atendente, Cliente
- [ ] Check-in/check-out no spa (validação de horário agendado)
- [ ] Histórico de visitas por cidadão
- [ ] Testes unitários + integração

### Projeto prático
**`pts-spa`** — controle de capacidade, agendamento por faixa e tipos de tratamento.

---

## Fase 14 — Segurança Pública

O jogo não tem uma estrutura formal de polícia/bombeiro — este módulo não tem análogo direto em Stardew Valley, então mantém nome genérico por transparência (forçar uma referência fraca do jogo aqui atrapalharia mais do que ajudaria).

### Tópicos
- [ ] Registro de ocorrências (furto, incêndio, resgate) com prioridade e gravidade
- [ ] Papéis: Policial, Bombeiro, Cidadão-denunciante
- [ ] Alocação de agentes para ocorrências
- [ ] Fluxo de status: aberta → em atendimento → encerrada
- [ ] Estatísticas de criminalidade por região/período
- [ ] Integração com Citizens: consulta de antecedentes, envolvidos em ocorrências
- [ ] Eventos de emergência publicados para outros módulos reagirem
- [ ] Testes unitários + integração

### Projeto prático
**`pts-seguranca-publica`** — ocorrências e fluxo de atendimento.

---

## Fase 15 — Guilda dos Aventureiros

Conectada à Segurança Pública (ocorrências na mina) e ao restante da cidade via quadro de recompensas — primeiro módulo do roadmap a introduzir **ranking/leaderboard**.

### Tópicos
- [ ] Quadro de recompensas (bounties) por captura de monstros/exploração da mina
- [ ] Ranking de cidadãos por recompensas cumpridas, com Redis Sorted Sets (leaderboard)
- [ ] Integração com Segurança Pública: ocorrência na mina pode gerar bounty automaticamente
- [ ] Testes unitários + integração (Redis em Testcontainer)

### Projeto prático
**`pts-guilda-aventureiros`** — bounty board e leaderboard em Redis.

---

## Fase 16 — Fazenda

A propriedade do próprio jogador.

### Tópicos
- [ ] Cadastro de propriedades rurais e plantios (relação com a Prefeitura para posse do lote)
- [ ] Ciclo de plantio/colheita por estação do ano (regra de domínio temporal)
- [ ] Tipos de fazenda (Standard, Riverland, Forest, Hill-top, Wilderness) afetando quais plantios/recursos são possíveis
- [ ] Estoque de produção agrícola, fornecido a JojaMart/Peixaria/Saloon via evento de "oferta disponível"
- [ ] Calendário de safras e previsão de colheita
- [ ] Testes unitários + integração

### Projeto prático
**`pts-fazenda`** — plantio, colheita e fornecimento aos outros módulos.

---

## Fase 17 — Torre do Mago Rasmodius

No jogo, o mago oferece serviços sobrenaturais e é guardião dos Junimos. Aqui vira um módulo de **rede de teleporte e serviços mágicos** — padrão único de grafo de localizações conectadas.

### Tópicos
- [ ] Rede de teleporte: grafos de localizações conectadas (ex: Torre → Montanha → Praia), com custo por salto
- [ ] Construção de cabanas Junimo (requer materiais de outros módulos — Carpintaria, Ferraria, Fazenda)
- [ ] Encantamentos e transmutação de itens
- [ ] Quests/missões mágicas com recompensas especiais
- [ ] Papéis: Mago Rasmodius, Cliente
- [ ] Testes unitários + integração

### Projeto prático
**`pts-torre-mago`** — rede de teleporte, cabanas Junimo, quests e encantamentos.

---

## Fase 18 — Hotel *(construção nova em Pelican Town)*

Diferente do Cassino de Calico (que não combina com a cidade), um hotel local introduz **reserva de quartos com ciclo de estadia** — padrão complementar ao agendamento da Clínica e diferente do controle de capacidade do Spa.

### Tópicos
- [ ] Tipos de quarto (standard, luxo, suíte) com diárias e capacidade
- [ ] Reserva: check-in/check-out com validação de disponibilidade por data
- [ ] Papéis: Hóspede, Recepcionista, Camareiro
- [ ] Serviços adicionais: café da manhã, lavanderia, passeios (excursão para Ilha Gengibre — Fase 21)
- [ ] Histórico de estadias por cidadão
- [ ] Faturamento e integração com impostos da Prefeitura
- [ ] Testes unitários + integração (cenários de overbooking)

### Projeto prático
**`pts-hotel`** — reservas, check-in/out, serviços de hospedagem.

---

## Fase 19 — Museu do Gunther

Primeiro módulo do roadmap desenhado para ser **puramente reativo**: nunca é chamado diretamente por outro módulo, só consome eventos.

### Tópicos
- [ ] Doação de artefatos/itens por cidadãos
- [ ] Coleções e conquistas (achievements) por progresso de doações
- [ ] Consome eventos de Peixaria (peixe raro capturado), Fazenda (colheita rara), Guilda (derrota de monstro) e Ferraria (minério raro) para sugerir doações automaticamente
- [ ] Event sourcing simples para o histórico de conquistas de cada cidadão
- [ ] Testes unitários (event handlers) + integração

### Projeto prático
**`pts-museu-gunther`** — coleções, conquistas e consumo de eventos de outros módulos.

---

## Fase 20 — Cinema

Só faz sentido narrativo depois que o Centro Comunitário está ativo (ou a JojaMart fechou), mas tecnicamente não depende de nenhum outro módulo. Introduz **sessões com lugares marcados** — variante de capacity booking com slots numerados.

### Tópicos
- [ ] Cartaz de filmes em cartaz com horários e salas
- [ ] Venda de ingressos com lugares marcados (poltrona A1, A2...)
- [ ] Capacidade por sala e verificação de disponibilidade por sessão
- [ ] Papéis: Espectador, Projecionista
- [ ] Sessões especiais (estreia, maratona temática)
- [ ] Histórico de filmes assistidos por cidadão
- [ ] Testes unitários + integração

### Projeto prático
**`pts-cinema`** — cartaz, sessões, ingressos com lugares marcados.

---

## Fase 21 — Ilha Gengibre (Ginger Island)

Turismo e comércio insular. O resort na ilha é uma extensão natural da economia de Pelican Town. Introduz **pacotes de viagem com múltiplos serviços** e **transporte agendado** (ferry).

### Tópicos
- [ ] Ferry/balsa: horários, capacidade, reserva de travessia
- [ ] Resort: estadias com pacotes (diária + atividades)
- [ ] Atividades insulares: pesca oceânica, exploração do vulcão, coleta de nozes douradas
- [ ] Comércio local da ilha (itens exclusivos, troca de nozes douradas)
- [ ] Papéis: Turista, Guia, Comerciante insular
- [ ] Consome eventos do Hotel (excursão contratada) e da Peixaria (pesca oceânica)
- [ ] Testes unitários + integração

### Projeto prático
**`pts-ilha-gengibre`** — ferry, resort, atividades e comércio insular.

---

## Fase 22 — Centro Comunitário

Assim como no jogo, é o módulo que "amarra" os outros — e simbolicamente marca a virada de JojaMart para uma economia mais comunitária.

### Tópicos
- [ ] Eventos da cidade (festivais, mutirões) com inscrição de cidadãos
- [ ] "Bundles"/metas coletivas: doações/contribuições de outros módulos contam para metas
- [ ] Agregador de eventos de todos os módulos anteriores — o consumidor mais dependente de mensageria funcionando bem
- [ ] Testes unitários (event handlers) + integração

### Projeto prático
**`pts-centro-comunitario`** — eventos da cidade e metas coletivas alimentadas por eventos dos demais módulos.

---

## Fase 23 — Mensageria e integração entre módulos

Com todos os módulos existindo, formalizar a comunicação assíncrona.

### Tópicos
- [ ] RabbitMQ + MassTransit (local/dev) — com abstração para trocar por Azure Service Bus em produção (Fase 33)
- [ ] Outbox Pattern para consistência entre DB e mensageria
- [ ] Eventos de integração por módulo (`CidadaoRegistrado`, `ConsultaAgendada`, `VendaRealizada`, `OcorrenciaAberta`, `ColheitaDisponivel`, `PeixeRaroCapturado`, `BountyCompleto`, `HospedeCheckIn`, etc.)
- [ ] Contratos de mensagens versionados (projeto compartilhado no Shared Kernel)
- [ ] Idempotência no consumo de eventos (IdempotencyKey)
- [ ] Dead-letter queue e retry policies (Polly)
- [ ] Refatorar a integração síncrona Carpintaria → Prefeitura (Fase 5) para Saga assíncrona
- [ ] Módulo transversal **Correios** (Mail): consome eventos de todos os módulos e simula envio de notificação (email/SMS) — o consumidor "genérico" que valida se a mensageria está bem desenhada
- [ ] Templates de notificação por evento
- [ ] Preferências de notificação por cidadão (quais canais, quais tipos de evento)
- [ ] Rate limit de notificações por cidadão (máx. N emails/hora)

### Projeto prático
**`pts-correios`** (novo, transversal) + eventos assíncronos substituindo chamadas diretas entre módulos onde fizer sentido.

---

## Fase 24 — Reporting & Read Models (CQRS Read Side)

Consolida dados de todos os módulos para dashboards e relatórios municipais.

### Tópicos
- [ ] Read models projetados via eventos (projeções nos consumidores)
- [ ] Dashboard do prefeito: receita municipal, criminalidade, saúde, educação, turismo
- [ ] Relatórios cross-module: perfil completo do cidadão (dados de Identity + Clinic + School + Market + Hotel)
- [ ] Cache de relatórios pesados em Redis
- [ ] Endpoints paginados com cursores (não offset) para grandes datasets
- [ ] Exportação CSV/Excel (CsvHelper, ClosedXML)
- [ ] Dapper para queries de agregação de alta performance
- [ ] Testes unitários (projeções) + integração

### Projeto prático
**`pts-reporting`** — dashboards, relatórios cross-module e exportação.

---

## Fase 25 — Decomposição em microsserviços

- [ ] API Gateway (YARP) com rate limiting por rota e por serviço downstream
- [ ] Extrair `pts-identity` como serviço independente (todo mundo depende dele)
- [ ] Gateway valida JWT e repassa claims via header para downstream services
- [ ] Extrair `pts-clinica-harvey` e `pts-jojamart` (domínios mais ricos e isolados)
- [ ] Service discovery (config estática ou DNS via Docker Compose)
- [ ] Comunicação síncrona entre serviços via gRPC para baixa latência (ex: JojaMart → Prefeitura para consulta de alíquota)
- [ ] Padrão Saga para fluxos que cruzam serviços (ex: alvará de obra da Carpintaria, ocorrência de incêndio que envolve Segurança Pública + JojaMart + Centro Comunitário, pacote de viagem Hotel + Ilha Gengibre)
- [ ] Health checks agregados no Gateway
- [ ] Testes de contrato entre serviços (PactNet — consumer-driven contracts)
- [ ] Webhooks: endpoint por serviço para que "sistemas externos" (ex: sistema da JojaCorp em Zuzu City) possam consumir eventos

### Projeto prático
Arquitetura final com Identity + 2-3 serviços de domínio extraídos — o restante permanece monólito modular. Decisão defensável em entrevista: nem tudo precisa ser microsserviço.

---

## Fase 26 — SignalR & Tempo Real

Tornar a plataforma viva com atualizações em tempo real via WebSocket.

### Tópicos
- [ ] **Guilda dos Aventureiros**: leaderboard atualizando ao vivo quando um bounty é completado
- [ ] **Stardrop Saloon**: comandas aparecendo na cozinha em tempo real (pedido → em preparo → pronto)
- [ ] **Segurança Pública**: painel de ocorrências com atualização ao vivo conforme status muda
- [ ] Autenticação no SignalR via JWT (mesmo token do Identity)
- [ ] Grupos por papel: `Cozinheiros` recebem eventos de pedido, `Policiais` de ocorrência, `Cidadaos` de leaderboard
- [ ] Fallback para long polling quando WebSocket não está disponível
- [ ] Backplane com Redis para suportar scale-out (múltiplas instâncias)
- [ ] Testes de integração com SignalR client

### Projeto prático
Hubs de SignalR nos módulos **Guilda**, **Saloon** e **Segurança Pública**. Biblioteca `BuildingBlocks/RealTime` com abstrações compartilhadas.

---

## Fase 27 — Multi-tenancy & Loja do Pierre

Trazer a Loja do Pierre das "expansões futuras" para o roadmap principal, demonstrando multi-tenancy com o mesmo código-base da JojaMart.

### Tópicos
- [ ] Estratégia de tenant isolation: schema-per-tenant no PostgreSQL (`tenant_jojamart`, `tenant_pierre`)
- [ ] Resolução de tenant via header `X-Tenant-Id` e subdomínio (`jojamart.pelicantown.local`, `pierre.pelicantown.local`)
- [ ] Middleware de tenant resolution no pipeline ASP.NET Core (antes da autorização)
- [ ] Mesma API serve ambos os comércios com dados completamente segregados
- [ ] Papéis: Caixa, Gerente (por tenant)
- [ ] Relatórios cross-tenant (admin da cidade — Prefeito Lewis) vs. intra-tenant (gerente de cada loja)
- [ ] Catálogo e preços independentes por loja (Pierre vende sementes, JojaMart vende de tudo)
- [ ] Testes de integração validando isolamento total de dados entre tenants

### Projeto prático
**`pts-general-store`** (Loja do Pierre) — mesmo assembly e código-base da JojaMart (`pts-jojamart`), tenant `pierre-general-store`.

---

## Fase 28 — Event Sourcing no Museu

Transformar o "event sourcing simples" do Museu em Event Sourcing completo com Event Store e projeções rebuildáveis.

### Tópicos
- [ ] Event Store em PostgreSQL: tabela `events` com `stream_id`, `version`, `event_type`, `data` (JSONB), `metadata` (JSONB)
- [ ] Append-only — eventos imutáveis, nunca atualizados ou deletados
- [ ] Projeções rebuildáveis: derrubar read model e reconstruir do zero via replay dos eventos
- [ ] Snapshots periódicos a cada N eventos por stream, para evitar replay completo
- [ ] Concorrência otimista no append: version check antes de escrever
- [ ] Read model materializado em Redis (cache) ou PostgreSQL (tabela de projeção)
- [ ] Testes: validação de que projeção rebuildada é idêntica à original
- [ ] Documentar ADR "Por que Event Sourcing no Museu e não no JojaMart?" — trade-off explícito

### Projeto prático
Refatoração do **`pts-museu-gunther`** — Event Store real, snapshots e projeções rebuildáveis. Biblioteca `BuildingBlocks/EventStore` reutilizável.

---

## Fase 29 — LGPD, Auditoria & Soft Delete (transversal)

Conformidade com a legislação brasileira e trilha de auditoria completa em todos os módulos.

### Tópicos
- [ ] `ISoftDeletable` implementado por todas as entidades com dados pessoais
- [ ] Campos de auditoria: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, `DeletedAt`, `DeletedBy`
- [ ] EF Core interceptor (`SaveChangesInterceptor`) preenche campos de auditoria automaticamente via HttpContext
- [ ] Trilha de auditoria em tabela `audit_logs`: entidade, operação (INSERT/UPDATE/DELETE), snapshot JSON antes/depois, IP, User-Agent, timestamp
- [ ] Endpoint `GET /api/me/data-export` — agrega dados do cidadão de todos os módulos (artigo 18, LGPD)
- [ ] Endpoint `DELETE /api/me/account` — soft-delete + anonimização de dados pessoais (nome → hash, email → anonimizado)
- [ ] Política de retenção: Quartz job que expurga dados após X anos conforme categoria
- [ ] Encriptação de dados sensíveis em repouso (prontuário médico, ocorrências policiais)
- [ ] Testes: soft delete não quebra FK, exportação agrega cross-module, anonimização irreversível, auditoria registra corretamente

### Projeto prático
Biblioteca compartilhada `BuildingBlocks/Auditability` + endpoints de LGPD no Identity.

---

## Fase 30 — Feature Flags & Distributed Locking

### Feature Flags
- [ ] `Microsoft.FeatureManagement` — habilitação/desabilitação de features em runtime sem redeploy
- [ ] Flags: `CinemaEnabled`, `IlhaGengibreEnabled`, `MuseuEventSourcingEnabled`, `FestivalVerao`
- [ ] Armazenamento: `appsettings.json` (dev) / Azure App Configuration (staging)
- [ ] Filtro customizado por tenant (feature ativa só para JojaMart, não para Pierre)
- [ ] Atributo `[FeatureGate]` em controllers/endpoints
- [ ] Endpoint administrativo para toggle de flags (só Admin)

### Distributed Locking
- [ ] Cenários críticos: fechamento mensal do JojaMart, cálculo semanal do ranking da Guilda, conciliação de estoque da Fazenda
- [ ] Implementação com Redis RedLock (DistributedLock.Redis)
- [ ] Timeout + heartbeat automático para locks longos
- [ ] Deadlock detection e release forçado em cenários de falha
- [ ] Testes: execução concorrente de múltiplas threads validando que lock impede race condition

### Projeto prático
`BuildingBlocks/FeatureFlags` e `BuildingBlocks/DistributedLocking`.

---

## Fase 31 — Segurança, resiliência e observabilidade

- [ ] OWASP Top 10 por serviço exposto (auditoria completa)
- [ ] Circuit Breaker e Retry com Polly (exponential backoff, timeout, bulkhead) entre microsserviços
- [ ] Rate limiting por serviço, não só no gateway
- [ ] OpenTelemetry: tracing distribuído (Gateway → serviços → banco/fila)
- [ ] Métricas customizadas com Prometheus + Grafana (latência p50/p95/p99, taxa de erro, throughput por endpoint)
- [ ] Dashboards por serviço e dashboard cross-service (visão do ecossistema)
- [ ] Health Checks UI com dashboard visual (AspNetCore.HealthChecks.UI)
- [ ] Secrets management (User Secrets em dev, Azure Key Vault em staging/prod)
- [ ] mTLS entre serviços com certificados autoassinados (dev) e gerenciados por Key Vault (staging)

---

## Fase 32 — Performance

- [ ] Profiling dos endpoints mais usados (`dotnet-trace`, `dotnet-counters`)
- [ ] Otimização de queries EF Core nos módulos de maior volume (JojaMart, Clínica, Hotel): AsNoTracking, SplitQuery, índices cobrindo queries reais
- [ ] Cache distribuído com Redis (catálogo da JojaMart/Pierre, disponibilidade de horários da Clínica, leaderboard da Guilda, mapa da rede de teleporte)
- [ ] Compression (Brotli/Gzip) nos responses
- [ ] Background Jobs com dashboard de monitoramento (Hangfire, acessível só por Admin)
- [ ] Testes de carga (k6) simulando picos nos módulos críticos
- [ ] Chaos Engineering leve: SimInject faults para validar circuit breakers em ação
- [ ] Documentar benchmarks de antes/depois das otimizações com gráficos

---

## Fase 33 — Azure Cloud & Infraestrutura como Código

Tirar o projeto do Docker local e colocar na nuvem com deploy profissional.

### Tópicos
- [ ] **Azure Container Apps** hospedando os microsserviços extraídos (Identity, Clínica, JojaMart/Pierre)
- [ ] **Azure Service Bus** como message broker em produção — MassTransit abstrai a troca (mesmo código, transportes diferentes por config)
- [ ] **Azure Blob Storage** para PDFs (boletos, boletins, notas fiscais) com lifecycle management
- [ ] **Azure Key Vault** — connection strings, JWK keys, API keys de terceiros
- [ ] **Managed Identity** — zero connection strings hardcoded, autenticação via identidade do container
- [ ] **Application Insights** como backend de OpenTelemetry (tracing + métricas)
- [ ] **Azure SQL** ou **Azure Database for PostgreSQL** (managed) para staging/prod
- [ ] **GitHub Actions** com federated credentials (OIDC) para deploy sem secrets duradouros
- [ ] **Terraform** — todos os recursos Azure provisionados como código (`infra/terraform/`)
- [ ] IaC versionado no repositório com pipeline: `terraform plan` no PR → `terraform apply` no merge para main
- [ ] Estratégia de resource groups e tags por ambiente (`staging`, `production`)

### Projeto prático
Diretório `infra/` com Terraform modules e GitHub Actions workflow de deploy para Azure.

### Estratégia de deploy para portfólio

O projeto completo (24 módulos) **não** sobe como 24 microsserviços independentes. O deploy de staging consolida assim:

| Serviço | Container | Motivo |
|---|---|---|
| Gateway (YARP) | 1 container | Roteamento |
| Identity API | 1 container | Foi extraído na Fase 25 |
| Clínica + JojaMart/Pierre | 2 containers | Extraídos na Fase 25, multi-tenant ativo |
| **Monólito modular** (20 módulos restantes) | 1 container | Processo único |

**Total: 5 containers + infra gerenciada**

Para entrevista: mostra o monólito funcionando e os 3 microsserviços extraídos rodando independentes. A decisão de "por que só 3 extraídos" vira ponto positivo na conversa.

### Plano de deploy gratuito (staging de portfólio)

| Recurso | Onde (free tier) | Custo |
|---|---|---|
| Compute (5 containers) | **fly.io** — 3 VMs compartilhadas (256 MB RAM cada) free | $0 |
| PostgreSQL | **Supabase** — 500 MB, 2 projetos free | $0 |
| Redis | **Upstash** — 256 MB, 10k comandos/dia free | $0 |
| Mensageria (RabbitMQ) | MassTransit **InMemory transport** — funcional para demo | $0 |
| PDFs e arquivos | **Cloudflare R2** — 10 GB armazenamento free | $0 |
| Domínio + HTTPS | **Cloudflare** DNS + Tunnel (certificado automático) | $0 |
| Keep-alive (cold start) | **UptimeRobot** — ping a cada 5 min nos containers | $0 |

**Total: $0/mês.** O InMemory transport do MassTransit funciona como RabbitMQ em escopo de processo — mensagens são publicadas e consumidas dentro do mesmo container. Para staging/demo é indistinguível do RabbitMQ real. Em prod (Azure, se subir) troca-se o transport para Azure Service Bus por configuração, sem alterar código.

Cold start do fly.io (containers hibernam após ~30 min de inatividade) é mitigado pelo health check ping do UptimeRobot, mantendo o container aquecido para quando um recrutador abrir o link.

---

## Fase 34 — CI/CD, deploy e documentação

- [ ] Dockerfile por serviço (multi-stage, distroless, non-root user)
- [ ] `docker-compose.yml` completo com todos os serviços + infra para desenvolvimento local
- [ ] GitHub Actions: pipeline completa (build → test → scan → push image → deploy staging → E2E tests → deploy prod)
- [ ] GitHub Container Registry (ghcr.io) para imagens Docker
- [ ] Kubernetes manifests para os serviços extraídos (Deployments, Services, Ingress, HPA)
- [ ] Helm charts como alternativa ao Terraform para deploy K8s
- [ ] Ambiente de staging acessível publicamente (link ao vivo no portfólio)
- [ ] `ARCHITECTURE.md` com ADRs documentando cada decisão arquitetural relevante:
  - Por que monólito modular → microsserviços e não microsserviços desde o início
  - Por que Event Sourcing no Museu e não em outros módulos
  - Por que database-per-service e não shared database
  - Por que schema-per-tenant e não database-per-tenant
  - Por que RabbitMQ em dev e Azure Service Bus em prod
  - Por que Identity extraído primeiro
- [ ] Diagrama de arquitetura C4 (System Context, Container, Component)
- [ ] Postman collection com todos os endpoints, organizada por módulo
- [ ] README.md completo: visão do produto, mapa dos 24 módulos, stack tecnológica, como rodar local e na cloud

---

## Visão geral dos módulos/serviços

| # | Módulo | Nome real em Stardew Valley? | Fase | Papéis principais | Vira microsserviço? | Conceito novo introduzido |
|---|---|---|---|---|---|---|
| 1 | Identity (Cidadãos + RBAC) | — (conceito transversal) | 1 | base de todos | Sim (Fase 25) | Auth, JWT, RBAC |
| 2 | Prefeitura | **Town Hall** ✅ | 3 | Prefeito Lewis | Não | Workflow de aprovação, impostos |
| 3 | Ferraria do Clint | **Blacksmith's** ✅ | 4 | Ferreiro | Não | Fila de serviço com SLA, Quartz.NET |
| 4 | Carpintaria do Robin | **Carpenter's Shop** ✅ | 5 | Carpinteiro | Não | Integração síncrona entre módulos |
| 5 | Clínica do Doutor Harvey | **Harvey's Clinic** ✅ | 6 | Médico, Recepcionista, Paciente | Sim (Fase 25) | Concorrência otimista, regras de acesso granulares |
| 6 | Rancho da Marnie | **Marnie's Ranch** ✅ | 7 | Fazendeiro, Veterinário | Não | Reuso de padrão (agendamento), sem acoplar código |
| 7 | JojaMart | **JojaMart** ✅ | 8 | Caixa, Gerente | Sim (Fase 25) | Estoque com concorrência, PDV, Redis |
| 8 | Escola | Schoolhouse (estrutura real) ⚠️ | 9 | Professor, Responsável | Não | Regras de domínio educacional, PDF |
| 9 | Peixaria do Willy | **Fish Shop** ✅ | 10 | Vendedor, Fornecedor | Não | Preços sazonais, eventos para consumo |
| 10 | Stardrop Saloon (Gus) | **The Stardrop Saloon** ✅ | 11 | Garçom, Cozinheiro, Cliente | Não | Comandas, fluxo de cozinha, evento semanal |
| 11 | Esgotos / Krobus | **The Sewers** ✅ | 12 | Krobus (vendedor), Cliente | Não | Reputação como desbloqueio de conteúdo |
| 12 | Spa / Casa de Banho | **Spa / Bath House** ✅ | 13 | Atendente, Cliente | Não | Controle de capacidade por faixa de horário |
| 13 | Segurança Pública | sem análogo direto ❌ | 14 | Policial, Bombeiro | Não | Fluxo de ocorrências com prioridade |
| 14 | Guilda dos Aventureiros | **Adventurer's Guild** ✅ | 15 | Aventureiro | Não | Leaderboard com Redis Sorted Sets |
| 15 | Fazenda | **Farm** ✅ | 16 | Fazendeiro | Não | Domínio temporal (estações), tipos de fazenda |
| 16 | Torre do Mago Rasmodius | **Wizard's Tower** ✅ | 17 | Mago, Cliente | Não | Rede de teleporte (grafo), quests mágicas |
| 17 | Hotel | — (construção nova) 🆕 | 18 | Hóspede, Recepcionista, Camareiro | Não | Reserva com check-in/out, ciclo de estadia |
| 18 | Museu do Gunther | **Museum** ✅ | 19 | Curador, Cidadão doador | Não | Módulo puramente reativo, event sourcing (Fase 28) |
| 19 | Cinema | **Movie Theater** ✅ | 20 | Espectador, Projecionista | Não | Sessões com lugares marcados |
| 20 | Ilha Gengibre | **Ginger Island** ✅ | 21 | Turista, Guia | Não | Pacotes de viagem, ferry agendado |
| 21 | Centro Comunitário | **Community Center** ✅ | 22 | Organizador, Cidadão | Não | Bundles/metas coletivas, agregador de eventos |
| 22 | Loja do Pierre | **Pierre's General Store** ✅ | 27 | Caixa, Gerente | Não | Multi-tenancy com mesmo código da JojaMart |
| 23 | Correios (Mail) | Mail system ⚠️ | 23 | — (transversal) | Não | Consumidor genérico, templates, preferências |
| 24 | Reporting | — (transversal) | 24 | — (transversal) | Não | Read models via CQRS, Dapper, exportação |

---

## Tecnologias utilizadas

| Tecnologia | Uso |
|---|---|
| .NET 9 | Runtime de todos os serviços |
| ASP.NET Core | Web APIs (Controllers + Minimal APIs) |
| SignalR | Tempo real (leaderboard ao vivo, cozinha, ocorrências) |
| Entity Framework Core | ORM principal (commands e queries simples) |
| Dapper | Queries de alta performance (Reporting, projeções pesadas) |
| PostgreSQL | Banco relacional — database-per-service no monólito, schema-per-tenant na multi-tenancy |
| Redis | Cache distribuído, refresh tokens, rate limiting, leaderboards, distributed locking |
| RabbitMQ + MassTransit | Mensageria, sagas e outbox pattern (dev/local) |
| Azure Service Bus | Message broker em produção (swap via config no MassTransit) |
| MediatR | CQRS (commands/queries) e pipeline behaviors |
| FluentValidation | Validação de inputs |
| Serilog + Seq | Logging estruturado com correlation-id |
| OpenTelemetry | Tracing distribuído e métricas (backend: Prometheus ou Application Insights) |
| YARP | API Gateway (reverse proxy) |
| gRPC | Comunicação síncrona de baixa latência entre microsserviços |
| Polly | Resiliência (circuit breaker, retry, timeout, bulkhead) |
| Quartz.NET / Hangfire | Background jobs recorrentes + dashboard de monitoramento |
| Microsoft.FeatureManagement | Feature flags |
| DistributedLock.Redis | Distributed locking (conciliação, fechamento mensal) |
| Docker + Kubernetes | Containerização e orquestração |
| Terraform / Bicep | Infraestrutura como Código (Azure) |
| Azure Container Apps | Hospedagem cloud dos microsserviços |
| Azure Blob Storage | Armazenamento de PDFs (boletos, boletins, notas fiscais) |
| Azure Key Vault | Secrets management em staging/prod |
| GitHub Actions | CI/CD |
| xUnit + Moq + AutoFixture | Testes unitários |
| Testcontainers | Testes de integração (Postgres, Redis, RabbitMQ reais) |
| PactNet | Testes de contrato entre microsserviços |
| k6 | Testes de carga |
| Prometheus + Grafana | Métricas e dashboards (dev/local) |
| Application Insights | APM em staging/prod (Azure) |
| QuestPDF | Geração de PDFs (boletos, recibos, boletins) |

---

## Por que essa ordem

1. **Identity primeiro** — nenhum outro módulo funciona sem cidadão/autenticação/RBAC. Já nasce com testes e CI.
2. **Town Hall (Prefeitura)** valida o padrão de módulo com baixo risco e já fornece o cadastro de impostos usado depois por JojaMart, Pierre e Hotel.
3. **Ferraria e Carpintaria logo depois** porque são domínios pequenos que introduzem, cada um, um conceito novo isolado (fila com SLA / dependência síncrona entre módulos) antes dos módulos pesados.
4. **Clínica e Rancho ficam juntos** para reforçar reuso de *padrão* (agendamento com concorrência) sem acoplar *código* — bom argumento de entrevista sobre desenho de domínio.
5. **JojaMart** entra depois, como módulo mais rico em regras transacionais (estoque, impostos, concorrência de venda).
6. **Escola e Peixaria** reforçam padrões já conhecidos em domínios novos.
7. **Saloon do Gus** introduz comandas e fluxo de cozinha — primeiro módulo com "pedidos em tempo real" e evento semanal recorrente.
8. **Esgotos e Spa** são módulos pequenos com conceitos únicos (reputação e capacity booking), baixo custo de implementação.
9. **Segurança, Guilda e Fazenda** trazem leaderboard (Redis Sorted Sets) e domínio temporal (estações).
10. **Torre do Mago** é o último módulo "independente" antes dos módulos reativos, introduzindo grafo de localizações.
11. **Hotel** combina padrões já conhecidos (reserva como Clínica, capacidade como Spa, faturamento como JojaMart) em um domínio novo e rico.
12. **Museu** é o primeiro módulo puramente reativo — consome eventos dos anteriores sem nunca ser chamado diretamente.
13. **Cinema** e **Ilha Gengibre** estendem a cidade com padrões complementares (lugares marcados, pacotes de viagem).
14. **Centro Comunitário** fecha os módulos de domínio como o grande agregador de eventos — simbolicamente a "conclusão" do jogo.
15. **Mensageria formalizada (Fase 23) só depois que todos os módulos existem** — os eventos de integração são desenhados sobre um domínio já validado, incluindo o Correios como consumidor genérico.
16. **Reporting (Fase 24)** com os dados de todos os módulos fluindo via eventos — projeções CQRS com dados reais.
17. **Microsserviços (Fase 25)** — extração consciente e defensável, não big-bang. Apenas 3 serviços saem do monólito. Webhooks + Pact entram aqui.
18. **SignalR (Fase 26) depois dos microsserviços** — WebSocket com backplane Redis só faz sentido quando há serviços independentes que precisam de estado compartilhado em tempo real.
19. **Multi-tenancy (Fase 27)** aplicada sobre um serviço já extraído (JojaMart) para demonstrar que o design de microsserviços isolados facilita multi-tenancy.
20. **Event Sourcing (Fase 28)** como refatoração do Museu — mostra evolução de arquitetura em um módulo existente, não greenfield.
21. **LGPD e Auditoria (Fase 29)** aplicados transversalmente — faz mais sentido quando todos os módulos já existem e produzem dados auditáveis.
22. **Feature Flags e Distributed Locking (Fase 30)** como última camada de infraestrutura antes de produção.
23. **Segurança e Observabilidade (Fase 31)** com toda a plataforma rodando — métricas e tracing fazem sentido com tráfego real.
24. **Performance (Fase 32)** — otimizar o que já funciona, com benchmarks de antes/depois. Hangfire dashboard aqui.
25. **Azure Cloud e IaC (Fase 33)** — último passo técnico: subir na nuvem com deploy profissional.
26. **CI/CD e Documentação (Fase 34)** — empacotar, documentar e publicar.

---

## Possíveis expansões futuras

- [ ] **Banco** — contas, transações, empréstimos, integração com Prefeitura para pagamento de impostos
- [ ] **Transporte público** — linhas, horários, bilhetagem (Minecart como metáfora)
- [ ] **Eleições** — candidatos, votação, apuração
- [ ] **Clima/Tempo** — integração com API externa, previsão para fazendeiros e pescadores
- [ ] **Feira Noturna (Night Market)** — evento sazonal com comerciantes temporários, pesca submarina
- [ ] **Deserto de Calico** — comerciantes exóticos, mineração, clube do deserto (expansão de alto nível)
