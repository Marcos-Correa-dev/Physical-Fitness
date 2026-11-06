# Physical Fitness 🏃‍♂️⏱️🏋️

> Sistema corporativo de monitoramento, telemetria de relógios inteligentes e auditoria de atividades físicas (Corrida, Ciclismo, Caminhada e Musculação). Possui conectividade **Bluetooth Low Energy (BLE)** para pareamento com dispositivos como **Xiaomi Mi Band 10**, **Garmin**, **Apple Watch** e **Polar**, mapa com imagens reais de **Satélite e Terreno do Google Maps**, backend em **.NET 8**, frontend em **SvelteKit** e banco de dados **PostgreSQL**.

---

## 🚀 Principais Funcionalidades

### 1. 📶 Conexão Bluetooth Low Energy (BLE)
- **Pareamento com Relógios e Pulseiras**: Suporte nativo à Web Bluetooth API do navegador com perfis GATT padrão (Heart Rate Service `0x180D`, Battery Service `0x180F`).
- **Suporte a Xiaomi Mi Band 10**: Detecção de status de bateria, nível de sinal (RSSI) e transmissão contínua de batimentos cardíacos ao vivo.
- **Processamento de Atividade Sugerida pelo Relógio**: O usuário pode acionar a ingestão do treino sugerido pelo relógio (ex: **Musculação / Treino de Força**, **Corrida** ou **Ciclismo**) com 1 clique direto da interface.

### 2. 🏋️ Suporte a Atividades de Musculação (Treino de Força)
- O motor analisa atividades estáticas em academias onde o deslocamento em KM é 0.0 km.
- Diferencia esforço de hipertrofia de outras modalidades: validação da resposta cardíaca anaeróbica (picos em séries de esforço) e densidade calórica sem exigir movimentação geodésica por satélite.

### 3. 🛰️ Mapa com Terreno e Satélite Real do Google Maps
- Renderização de camadas do Google Maps:
    - **Google Satélite Híbrido Real**: Fotografia aérea de satélite em alta resolução com rótulos de ruas e avenidas.
    - **Google Terreno**: Sombreamento físico de relevo, topografia, elevações e curvas de nível.
    - **Mapa Urbano**: Linhas viárias limpas para análise de rotas urbanas.
- Ponto fixo em academia/centro de treinamento com marcador especial para treinos de musculação indoor.

### 4. 📊 Dashboard Analítico Completo
- **Dia, Data e Hora em Tempo Real**: Calendário e relógio síncronos no cabeçalho e registros de cada treino.
- **KM Circulado e Validado**: Comparativo entre a distância reportada pelo sensor do relógio e a distância recalculada via satélite.
- **Calorias Queimadas**: Contador de gasto energético ativo.
- **Frequência Cardíaca**: Média e picos de bpm com indicação de zonas metabólicas.
- **Laudo de Auditoria da Atividade**: Interface limpa e amigável sem jargões de desenvolvimento, permitindo inspecionar cada critério e reavaliar atividades sob demanda.

### 5. 🌐 Internacionalização (i18n)
- **Suporte a Português (pt-BR) e Inglês (en)**: Toda a aplicação conta com suporte completo a internacionalização.
- **Seletor de Idioma**: Botões rápidos no cabeçalho (`🇧🇷 PT` / `🇺🇸 EN`) com feedback visual reativo.
- **Persistência Automática**: Preferência salva no `localStorage` e atributo `html[lang]` sincronizado em tempo real.
- **Formatação Dinâmica de Datas e Horas**: Dias da semana, datas e horários adaptam-se dinamicamente conforme o idioma ativo (`toLocaleDateString` / `toLocaleTimeString`).
- **Tradução de Modalidades e Status**: Corrida/Running, Ciclismo/Cycling, Musculação/Strength Training, Válido/Valid, Suspeito/Suspect, Rejeitado/Rejected.

---

## 🏛️ Arquitetura do Sistema

```
backend/
├── src/
│   ├── KmValidator.Domain/               # Núcleo de Domínio (Entidades, Value Objects, Regras Fisiológicas)
│   │   ├── Entities/                     # PhysicalActivity, Athlete, WearableDevice, RoutePoint
│   │   ├── ValueObjects/                 # Distance, Coordinates (Haversine), HeartRate, Pace, CaloricExpenditure
│   │   ├── Enums/                        # ActivityType (Corrida, Ciclismo, Caminhada, Musculação)
│   │   └── Services/Validation/          # Validações geodésicas, ritmo humano e biometria
│   ├── KmValidator.Application/          # Casos de Uso e DTOs de Telemetria
│   │   └── UseCases/                     # Ingestão, Simulação Bluetooth e Métricas do Dashboard
│   ├── KmValidator.Infrastructure/       # Persistência PostgreSQL EF Core e Repositórios
│   └── KmValidator.Api/                  # API REST ASP.NET Core .NET 8
└── tests/
    └── KmValidator.UnitTests/            # Testes automatizados (5 testes cobrindo corrida, carro e musculação)
frontend/
└── src/
    ├── lib/
    │   ├── components/                   # DashboardHeader, RouteMap (Google Maps), BluetoothConnectModal, ActivityAuditModal
    │   └── services/                     # Integração com API REST
    └── routes/                           # Dashboard SvelteKit (+page.svelte)
```

---

## 🗄️ Banco de Dados PostgreSQL

Persistência na base `km_validator_db` com tipos e relacionamentos:
- **`athletes`**: Atleta cadastrado.
- **`wearable_devices`**: Relógios cadastrados (Xiaomi Mi Band 10, Garmin, Apple, Polar) com FK para o atleta.
- **`physical_activities`**: Atividades com status (`Valid`, `Suspect`, `Rejected`), KM, calorias e batimentos.
- **`route_points`**: Coordenadas geográficas, elevação, velocidade e bpm ponto a ponto.
- **`activity_validations`**: Parecer algorítmico e discrepância percentual.
- **`validation_issues`**: Registros de avisos ou inconformidades detectadas.

---

## 🚀 Como Executar

### 1. Iniciar o Backend (.NET 8)
```powershell
dotnet run --project backend/src/KmValidator.Api/KmValidator.Api.csproj --urls "http://localhost:5005"
```
- API REST: `http://localhost:5005`
- Swagger UI: `http://localhost:5005/swagger`

### 2. Iniciar o Frontend (SvelteKit)
```powershell
cd frontend
npm run dev
```
- Web Dashboard: `http://localhost:5173`
