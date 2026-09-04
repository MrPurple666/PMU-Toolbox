# Segurança

- A senha de contas locais é armazenada somente como hash Argon2id; senha de domínio nunca é persistida.
- `POST /api/v1/login` emite apenas o token de sessão aleatório em cookie HttpOnly, SameSite Strict, com validade de oito horas; o banco guarda somente SHA-256 do token.
- Em produção, a API exige principal confiável de Windows Integrated Authentication/Kerberos no proxy reverso ou sessão local autenticada. Sem principal, responde não autorizado.
- O servidor nunca eleva RBAC com base em `nomeUsuario` recebido no JSON.
- URLs, caminhos, extensões, executáveis e argumentos são validados em uma única camada antes da abertura.
- `ComandoControlado` aceita somente identificador de comando previamente permitido pelo servidor. Texto recebido nunca é executado por `cmd.exe /c` ou PowerShell remoto.
- Logs e eventos de auditoria não registram senha, token completo, cookie, cabeçalho `Authorization` ou credenciais LDAP; o sanitizador remove chaves contendo `senha`, `token` ou `password`.
- O adaptador simulado de identidade só é habilitado explicitamente em desenvolvimento; LDAP/AD permanece adaptador isolado no MVP.
