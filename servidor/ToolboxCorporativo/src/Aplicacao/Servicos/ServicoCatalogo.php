<?php

declare(strict_types=1);

namespace App\Aplicacao\Servicos;

use App\Dominio\Entidades\Recurso;
use App\Infraestrutura\Persistencia\BancoDados;
use App\Infraestrutura\Persistencia\RepositorioRecursos;
use InvalidArgumentException;
use PDO;

final readonly class ServicoCatalogo
{
    public function __construct(
        private BancoDados $banco,
        private RepositorioRecursos $recursos,
        private ServicoAutenticacaoLocal $autenticacao,
    ) {}

    /** @param array<string, mixed> $dados */
    public function sincronizar(string $principal, array $dados): array
    {
        $usuario = $this->obterOuCriarUsuario($principal);
        $usuarioId = (string) $usuario['id'];
        $nomeExibicao = (string) $usuario['nome_exibicao'];
        $computador = $dados['computador'] ?? [];
        if (!is_array($computador)) {
            throw new InvalidArgumentException('O campo computador deve ser um objeto.');
        }
        $nomeComputador = trim((string) ($computador['nome'] ?? ''));
        if ($nomeComputador === '') {
            throw new InvalidArgumentException('O nome do computador é obrigatório.');
        }
        $cliente = $dados['cliente'] ?? [];
        if (!is_array($cliente)) {
            throw new InvalidArgumentException('O campo cliente deve ser um objeto.');
        }
        $this->registrarComputador($nomeComputador, (string) ($computador['versaoWindows'] ?? 'desconhecida'), $usuarioId, (string) ($cliente['versao'] ?? 'desconhecida'));
        return $this->configuracao($usuarioId, $nomeExibicao);
    }

    public function configuracaoDoPrincipal(string $principal): array
    {
        $usuario = $this->obterOuCriarUsuario($principal);
        return $this->configuracao((string) $usuario['id'], (string) $usuario['nome_exibicao']);
    }
    public function usuarioDoPrincipal(string $principal): array
    {
        return $this->obterOuCriarUsuario($principal);
    }
    public function principalDaRequisicao(): string
    {
        return $this->autenticacao->principalDaRequisicao();
    }

    public function podeAdministrar(string $principal): bool
    {
        if (trim($principal) === '') {
            return false;
        }
        return in_array($this->obterOuCriarUsuario($principal)['perfil'], ['Superadministrador', 'Administrador', 'GestorDeSetor'], true);
    }
    public function metricasDashboard(): array
    {
        $conexao = $this->banco->obterConexao();
        return [
            'usuariosAtivos' => (int) $conexao->query('SELECT COUNT(*) FROM usuarios WHERE ativo = 1')->fetchColumn(),
            'computadores' => (int) $conexao->query('SELECT COUNT(*) FROM computadores')->fetchColumn(),
            'recursos' => (int) $conexao->query('SELECT COUNT(*) FROM recursos WHERE ativo = 1')->fetchColumn(),
            'auditoria' => (int) $conexao->query('SELECT COUNT(*) FROM eventos_auditoria')->fetchColumn(),
        ];
    }

    /** @param array<string, mixed> $dados */
    public function salvarRecurso(array $dados, string $autorId): Recurso
    {
        $conexao = $this->banco->obterConexao();
        $conexao->beginTransaction();
        try {
            $recurso = $this->recursos->salvar($dados);
            $this->registrarVersao($conexao, $autorId, 'recurso.salvo', $recurso->id, null, $recurso->paraResposta());
            $conexao->commit();
            return $recurso;
        } catch (\Throwable $erro) {
            $conexao->rollBack();
            throw $erro;
        }
    }

    public function desativarRecurso(string $id, string $autorId): void
    {
        $conexao = $this->banco->obterConexao();
        $conexao->beginTransaction();
        try {
            $this->recursos->desativar($id);
            $this->registrarVersao($conexao, $autorId, 'recurso.desativado', $id, ['ativo' => true], ['ativo' => false]);
            $conexao->commit();
        } catch (\Throwable $erro) {
            $conexao->rollBack();
            throw $erro;
        }
    }
    /** @param list<string> $ids */
    public function desativarEmMassa(array $ids, string $autorId): int
    {
        $ids = array_values(array_filter(array_unique($ids), static fn (string $id): bool => $id !== ''));
        if ($ids === []) {
            throw new InvalidArgumentException('Nenhum recurso informado.');
        }
        $conexao = $this->banco->obterConexao();
        $conexao->beginTransaction();
        try {
            $quantidade = 0;
            foreach ($ids as $id) {
                $stmt = $conexao->prepare('UPDATE recursos SET ativo = 0, atualizado_em = :data WHERE id = :id AND ativo = 1');
                $stmt->execute(['data' => gmdate('c'), 'id' => $id]);
                if ($stmt->rowCount() > 0) {
                    $quantidade++;
                    $this->registrarVersao($conexao, $autorId, 'recurso.desativado', $id, ['ativo' => true], ['ativo' => false]);
                }
            }
            $conexao->commit();
            return $quantidade;
        } catch (\Throwable $erro) {
            $conexao->rollBack();
            throw $erro;
        }
    }

    public function atribuir(string $recursoId, string $tipoAlvo, ?string $alvoId, string $estado, string $autorId): void
    {
        if ($tipoAlvo === 'Todos' && $alvoId !== null) {
            throw new InvalidArgumentException('A atribuição para todos não possui alvo.');
        }
        if (!in_array($estado, ['Disponivel', 'Obrigatorio', 'Bloqueado'], true)) {
            throw new InvalidArgumentException('Estado de atribuição inválido.');
        }
        $conexao = $this->banco->obterConexao();
        $conexao->beginTransaction();
        try {
            $stmt = $conexao->prepare(<<<'SQL'
                INSERT INTO atribuicoes (id, recurso_id, tipo_alvo, alvo_id, estado, herdada, criado_em)
                VALUES (:id, :recurso_id, :tipo_alvo, :alvo_id, :estado, 0, :criado_em)
                ON CONFLICT(recurso_id, tipo_alvo, alvo_id) DO UPDATE SET estado = excluded.estado, herdada = 0
                SQL);
            $stmt->execute([
                'id' => self::uuid(),
                'recurso_id' => $recursoId,
                'tipo_alvo' => $tipoAlvo,
                'alvo_id' => $alvoId,
                'estado' => $estado,
                'criado_em' => gmdate('c'),
            ]);
            $this->registrarVersao($conexao, $autorId, 'atribuicao.alterada', $recursoId, null, compact('tipoAlvo', 'alvoId', 'estado'));
            $conexao->commit();
        } catch (\Throwable $erro) {
            $conexao->rollBack();
            throw $erro;
        }
    }
    public function restaurarVersao(int $versao, string $autorId): void
    {
        $conexao = $this->banco->obterConexao();
        $stmt = $conexao->prepare('SELECT payload_json FROM versoes_configuracao WHERE versao = :versao');
        $stmt->execute(['versao' => $versao]);
        $payload = $stmt->fetchColumn();
        if (!is_string($payload)) {
            throw new InvalidArgumentException('Versão não encontrada.');
        }
        $dados = json_decode($payload, true, flags: JSON_THROW_ON_ERROR);
        if (!is_array($dados) || !isset($dados['id'], $dados['nome'], $dados['destino'])) {
            throw new InvalidArgumentException('A versão não contém um recurso restaurável.');
        }
        /** @var array<string, mixed> $dados */
        $dados['ativo'] = true;
        $conexao->beginTransaction();
        try {
            $recurso = $this->recursos->salvar($dados);
            $this->registrarVersao($conexao, $autorId, 'recurso.restaurado', $recurso->id, null, $recurso->paraResposta());
            $conexao->commit();
        } catch (\Throwable $erro) {
            $conexao->rollBack();
            throw $erro;
        }
    }

    /** @return list<Recurso> */
    public function listarRecursos(): array
    {
        return $this->recursos->listarTodos();
    }

    /** @return array<string, mixed> */
    private function configuracao(string $usuarioId, string $nomeExibicao): array
    {
        $recursos = $this->recursos->listarAtivos();
        $conexao = $this->banco->obterConexao();
        $alvos = [
            'Todos' => [],
            'Usuario' => [$usuarioId],
            'GrupoInterno' => [],
            'GrupoLdap' => [],
            'Setor' => [],
            'Computador' => [],
            'ConjuntoComputadores' => [],
        ];
        $stmt = $conexao->prepare('SELECT tipo, grupo_id FROM usuarios_grupos INNER JOIN grupos ON grupos.id = usuarios_grupos.grupo_id WHERE usuario_id = :usuario');
        $stmt->execute(['usuario' => $usuarioId]);
        /** @var list<array<string, mixed>> $grupos */
        $grupos = $stmt->fetchAll();
        foreach ($grupos as $grupo) {
            $alvos[(string) $grupo['tipo']][] = (string) $grupo['grupo_id'];
        }
        $stmt = $conexao->prepare('SELECT setor_id FROM usuarios WHERE id = :usuario');
        $stmt->execute(['usuario' => $usuarioId]);
        $setorId = $stmt->fetchColumn();
        if (is_string($setorId) && $setorId !== '') {
            $alvos['Setor'][] = $setorId;
        }
        $stmt = $conexao->prepare('SELECT id FROM computadores WHERE usuario_recente_id = :usuario ORDER BY ultima_sincronizacao DESC LIMIT 1');
        $stmt->execute(['usuario' => $usuarioId]);
        $computadorId = $stmt->fetchColumn();
        if (is_string($computadorId) && $computadorId !== '') {
            $alvos['Computador'][] = $computadorId;
            $stmt = $conexao->prepare('SELECT conjunto_id FROM computadores_conjuntos WHERE computador_id = :computador');
            $stmt->execute(['computador' => $computadorId]);
            /** @var list<array<string, mixed>> $conjuntos */
            $conjuntos = $stmt->fetchAll();
            foreach ($conjuntos as $conjunto) {
                $alvos['ConjuntoComputadores'][] = (string) $conjunto['conjunto_id'];
            }
        }
        $condicoes = ["tipo_alvo = 'Todos'", "(tipo_alvo = 'Usuario' AND alvo_id = :usuario)"];
        $parametros = ['usuario' => $usuarioId];
        foreach ($alvos as $tipo => $ids) {
            if ($ids === [] || $tipo === 'Todos' || $tipo === 'Usuario') {
                continue;
            }
            $marcadores = [];
            foreach (array_values(array_unique($ids)) as $indice => $id) {
                $marcador = ':' . strtolower($tipo) . $indice;
                $marcadores[] = $marcador;
                $parametros[$marcador] = $id;
            }
            $condicoes[] = "(tipo_alvo = '" . $tipo . "' AND alvo_id IN (" . implode(',', $marcadores) . '))';
        }
        $stmt = $conexao->prepare('SELECT * FROM atribuicoes WHERE ' . implode(' OR ', $condicoes));
        $stmt->execute($parametros);
        /** @var list<array<string, mixed>> $atribuicoes */
        $atribuicoes = $stmt->fetchAll();
        /** @var array<string, list<array<string, mixed>>> $porRecurso */
        $porRecurso = [];
        foreach ($atribuicoes as $atribuicao) {
            $porRecurso[(string) $atribuicao['recurso_id']][] = $atribuicao;
        }

        $respostas = [];
        $politica = (bool) $this->banco->obterConexao()->query('SELECT precedencia_usuario_grupo FROM politicas WHERE id = 1')->fetchColumn();
        foreach ($recursos as $recurso) {
            $candidatas = $porRecurso[$recurso->id] ?? [];
            $bloqueios = array_filter($candidatas, static fn (array $item): bool => ($item['estado'] ?? '') === 'Bloqueado');
            $candidatas = $bloqueios !== [] ? array_values($bloqueios) : $candidatas;
            $usuarioExplicito = array_filter($candidatas, static fn (array $item): bool => ($item['tipo_alvo'] ?? '') === 'Usuario' && !(bool) ($item['herdada'] ?? false) && ($item['estado'] ?? '') !== 'Bloqueado');
            if ($politica && $usuarioExplicito !== []) {
                $candidatas = array_values(array_filter($candidatas, static fn (array $item): bool => ($item['tipo_alvo'] ?? '') === 'Usuario'));
            }
            usort($candidatas, static function (array $a, array $b): int {
                $estado = ['Disponivel' => 1, 'Obrigatorio' => 2, 'Bloqueado' => 3];
                $escopo = ['Todos' => 0, 'Setor' => 1, 'GrupoInterno' => 2, 'GrupoLdap' => 2, 'ConjuntoComputadores' => 3, 'Computador' => 4, 'Usuario' => 5];
                $comparacao = ($estado[(string) ($b['estado'] ?? '')] ?? 0) <=> ($estado[(string) ($a['estado'] ?? '')] ?? 0);
                if ($comparacao !== 0) {
                    return $comparacao;
                }
                $comparacao = ($escopo[(string) ($b['tipo_alvo'] ?? '')] ?? 0) <=> ($escopo[(string) ($a['tipo_alvo'] ?? '')] ?? 0);
                return $comparacao !== 0 ? $comparacao : strcmp((string) ($a['id'] ?? ''), (string) ($b['id'] ?? ''));
            });
            $respostas[] = $recurso->paraResposta((string) ($candidatas[0]['estado'] ?? 'Herdado'));
        }

        $politicas = $this->banco->obterConexao()->query('SELECT precedencia_usuario_grupo AS precedenciaUsuarioSobreHerancaGrupo, descoberta_automatica AS descobertaAutomatica, intervalo_sincronizacao_segundos AS intervaloSincronizacaoSegundos FROM politicas WHERE id = 1')->fetch();
        return [
            'versao' => (int) $this->banco->obterConexao()->query('SELECT MAX(versao) FROM versoes_configuracao')->fetchColumn(),
            'usuario' => ['nomeExibicao' => $nomeExibicao],
            'recursos' => $respostas,
            'politicas' => is_array($politicas) ? $politicas : [],
        ];
    }

    /** @return array<string, mixed> */
    private function obterOuCriarUsuario(string $principal): array
    {
        $partes = str_contains($principal, '\\') ? explode('\\', $principal, 2) : ['LOCAL', $principal];
        $dominio = $partes[0] ?? 'LOCAL';
        $nome = $partes[1] ?? $principal;
        $conexao = $this->banco->obterConexao();
        $stmt = $conexao->prepare('SELECT * FROM usuarios WHERE nome_usuario = :nome LIMIT 1');
        $stmt->execute(['nome' => $nome]);
        /** @var array<string, mixed>|false $usuario */
        $usuario = $stmt->fetch();
        if ($usuario !== false) {
            return $usuario;
        }
        $perfil = getenv('APP_ENV') === 'dev' && getenv('TOOLBOX_IDENTIDADE_SIMULADA') === '1'
            ? (getenv('TOOLBOX_PERFIL_SIMULADO') ?: 'Usuario')
            : 'Usuario';
        $usuario = ['id' => self::uuid(), 'nome_usuario' => $nome, 'nome_exibicao' => $nome, 'dominio' => $dominio, 'perfil' => $perfil];
        $stmt = $conexao->prepare("INSERT INTO usuarios (id, nome_usuario, nome_exibicao, dominio, perfil, ativo, criado_em, atualizado_em) VALUES (:id, :nome_usuario, :nome_exibicao, :dominio, :perfil, 1, :data, :data)");
        $stmt->execute($usuario + ['data' => gmdate('c')]);
        return $usuario;
    }

    private function registrarComputador(string $nome, string $versaoWindows, string $usuarioId, string $toolboxVersao): void
    {
        $stmt = $this->banco->obterConexao()->prepare(<<<'SQL'
            INSERT INTO computadores (id, nome, versao_windows, usuario_recente_id, toolbox_versao, ultima_sincronizacao)
            VALUES (:id, :nome, :versao_windows, :usuario, :toolbox, :data)
            ON CONFLICT(nome) DO UPDATE SET versao_windows = excluded.versao_windows, usuario_recente_id = excluded.usuario_recente_id, toolbox_versao = excluded.toolbox_versao, ultima_sincronizacao = excluded.ultima_sincronizacao
            SQL);
        $stmt->execute(['id' => self::uuid(), 'nome' => $nome, 'versao_windows' => $versaoWindows, 'usuario' => $usuarioId, 'toolbox' => $toolboxVersao, 'data' => gmdate('c')]);
    }

    private function registrarVersao(PDO $conexao, string $autorId, string $acao, string $objetoId, ?array $anterior, ?array $novo): void
    {
        $versao = ((int) $conexao->query('SELECT MAX(versao) FROM versoes_configuracao')->fetchColumn()) + 1;
        $conexao->prepare('INSERT INTO versoes_configuracao (versao, criado_em, autor_id, payload_json) VALUES (:versao, :data, :autor, :payload)')->execute([
            'versao' => $versao,
            'data' => gmdate('c'),
            'autor' => $autorId,
            'payload' => json_encode($novo ?? [], JSON_THROW_ON_ERROR),
        ]);
        $anteriorSeguro = $anterior === null ? null : self::sanitizarAuditoria($anterior);
        $novoSeguro = $novo === null ? null : self::sanitizarAuditoria($novo);
        $conexao->prepare('INSERT INTO eventos_auditoria (id, autor_id, criado_em, acao, objeto_tipo, objeto_id, valor_anterior_json, valor_novo_json) VALUES (:id, :autor, :data, :acao, :tipo, :objeto, :anterior, :novo)')->execute([
            'id' => self::uuid(),
            'autor' => $autorId,
            'data' => gmdate('c'),
            'acao' => $acao,
            'tipo' => 'catalogo',
            'objeto' => $objetoId,
            'anterior' => $anteriorSeguro === null ? null : json_encode($anteriorSeguro, JSON_THROW_ON_ERROR),
            'novo' => $novoSeguro === null ? null : json_encode($novoSeguro, JSON_THROW_ON_ERROR),
        ]);
    }
    /** @param array<array-key, mixed> $dados
     * @return array<array-key, mixed>
     */
    private static function sanitizarAuditoria(array $dados): array
    {
        $seguro = [];
        foreach ($dados as $chave => $valor) {
            if (preg_match('/senha|token|password/i', (string) $chave) === 1) {
                continue;
            }
            $seguro[$chave] = is_array($valor) ? self::sanitizarAuditoria($valor) : $valor;
        }
        return $seguro;
    }

    private static function uuid(): string
    {
        $bytes = random_bytes(16);
        $bytes[6] = chr((ord($bytes[6]) & 0x0f) | 0x40);
        $bytes[8] = chr((ord($bytes[8]) & 0x3f) | 0x80);
        return vsprintf('%s%s-%s-%s-%s-%s%s%s', str_split(bin2hex($bytes), 4));
    }
}
