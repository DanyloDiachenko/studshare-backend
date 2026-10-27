# StudShare — Backend

Серверна частина закритої платформи для студентської групи **«Платформа для структурування навчальних конспектів»**
(ІП-44, НТУУ «КПІ ім. Ігоря Сікорського», дисципліна «Життєвий цикл розробки ПЗ»).

Студенти однієї групи зберігають конспекти в ієрархічному файловому сховищі, діляться ними й бачать журнал змін.
Доступ розмежовано за ролями: **студент**, **староста**, **студкуратор**.

## Технології

| Шар | Технологія |
|---|---|
| Платформа | .NET 10, ASP.NET Core Minimal API |
| База даних | PostgreSQL 17, EF Core 10 (Npgsql), міграції |
| Автентифікація | JWT Bearer, паролі — BCrypt |
| Валідація | FluentValidation |
| Документація API | OpenAPI + Swagger UI (у Development) |
| Контейнеризація | Docker, Docker Compose |
| Тести | xUnit |

## Структура

```
src/
  StudShare.Api/             точка входу та поточна реалізація
    Endpoints/               HTTP-модулі: Auth, Profile, Group, Files(+Folders), Members(+Invitations), History
    Services/                бізнес-логіка (AuthService, GroupService, FileService, FolderService, MemberService,
                             InvitationService, HistoryService, EmailService, EmailVerificationService, CurrentUserService)
    Entities/                User, Group, Folder, File, GroupInvitation, HistoryEvent, EmailVerificationCode
    DTos/                    record-типи запитів і відповідей
    Validators/              FluentValidation-валідатори
    Data/                    AppDbContext, Configurations/, Migrations/
    Program.cs               DI, JWT, OpenAPI, підключення модулів
  StudShare.Domain/          (зарезервовано) доменна модель
  StudShare.Application/     (зарезервовано) інтерфейси та use-case
  StudShare.Infrastructure/  (зарезервовано) БД, сховище, пошта
tests/StudShare.UnitTests/   xUnit-тести
docs/                        звіти ЛР1–ЛР4, UML-діаграми
```

Архітектура — модульний моноліт: кожному сервісу з проєкту архітектури (Auth, User, Group, Folder, File)
відповідає окремий модуль «сервіс + endpoints + DTO + валідатори».

## Ролі

| Роль | Можливості |
|---|---|
| Студент | Створює папки, завантажує файли (якщо `UploadAccess`), скачує, шукає, редагує свій профіль |
| Староста | Усе, що студент; закріплює файли й папки; перейменовує чужі; вмикає/вимикає завантаження студентам |
| Студкуратор | Створює групу; запрошує й виключає учасників; призначає старосту; редагує дані учасників і групи; видаляє групу |

## API (префікси)

`/api/auth` · `/api/profile` · `/api/groups` · `/api/files` · `/api/folders` · `/api/members` · `/api/invitations` · `/api/history`

Повний опис — у Swagger UI (`/swagger`) після запуску в режимі Development.

## Запуск

### Змінні середовища

Створіть файл `.env` у корені репозиторію (він у `.gitignore`):

```env
DB_PASSWORD=your_db_password
JWT_SECRET=long_random_string_at_least_32_chars
EMAIL_HOST=smtp.example.com
EMAIL_PORT=587
EMAIL_USER=user@example.com
EMAIL_PASSWORD=smtp_password
EMAIL_FROM=StudShare <user@example.com>
```

### Локально

```bash
# 1. PostgreSQL (наприклад, лише БД з compose)
docker compose up -d db

# 2. Міграції
dotnet tool install --global dotnet-ef        # один раз
dotnet ef database update --project src/StudShare.Api

# 3. Запуск API → http://localhost:5239, Swagger → /swagger
dotnet run --project src/StudShare.Api
```

### Docker Compose (БД + API)

```bash
docker compose up --build        # API на http://localhost:8080
```

Міграції застосовуються окремо командою `dotnet ef database update` (див. вище; БД доступна на `localhost:5432`).

### Тести

```bash
dotnet test
```

## Команда та ролі

| Учасник | Роль | Зона відповідальності |
|---|---|---|
| Давиденко Г.А. | Backend | Services, Endpoints, Entities, Data, автентифікація, ієрархія ролей, Docker |
| Дяченко Д.О. | Frontend | Клієнтська частина (репозиторій `studshare`) |
| Настенко А.Г. | QA | Тести, тест-кейси ролей і токенів, CI |

## Робота з Git

- `main` — стабільна гілка, зміни лише через pull request;
- `develop` — інтеграційна гілка;
- `feature/<модуль>-<назва>` — гілка на кожну задачу з дошки Trello;
- перед злиттям потрібне схвалення (code review) іншого учасника;
- повідомлення комітів: `feat|fix|docs|test|refactor: короткий опис (#задача)`.

## Стан реалізації

Реалізовано: реєстрація й вхід (JWT), підтвердження email і скидання пароля кодом, профіль, створення/зміна/видалення групи,
запрошення, ролі й право завантаження, ієрархія папок, завантаження/скачування/перейменування/видалення/закріплення файлів,
пошук за назвою, журнал подій.

Заплановано (ЛР5): винесення інтерфейсів (`IFileStorage`, `IFileLimitPolicy`, `IHistoryLogger`) у Application/Infrastructure,
автоматичні тести, підключення зовнішнього сховища та брокера подій, оптимізація та перевірка нефункціональних вимог.
