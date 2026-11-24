-- Misma cuenta: no crea usuarios ni cambia contraseñas, roles o claims.
-- Aplicar con la aplicación detenida, respaldo previo y versión compatible con sagdi.
\set ON_ERROR_STOP on
BEGIN;
SELECT set_config('sicom.rename_expected_database', :'base_objetivo', true);
SELECT pg_advisory_xact_lock(hashtext('DEMO'));
SELECT pg_advisory_xact_lock(hashtext('SAGDI'));
LOCK TABLE "AspNetUsers" IN SHARE ROW EXCLUSIVE MODE;
DO $$
DECLARE
 cuenta_id text;
 destino_id text;
 antes jsonb;
 despues jsonb;
BEGIN
 IF current_database() <> current_setting('sicom.rename_expected_database') THEN RAISE EXCEPTION 'La base conectada no coincide con base_objetivo. No se modifica nada.'; END IF;
 IF to_regclass('public."AuditLogs"') IS NULL THEN RAISE EXCEPTION 'Falta AuditLogs. Primero integrar la versión de auditoría.'; END IF;
 IF (SELECT count(*) FROM "AspNetUsers" WHERE "NormalizedUserName"='DEMO' OR lower("UserName")='demo')>1
 OR (SELECT count(*) FROM "AspNetUsers" WHERE "NormalizedUserName"='SAGDI' OR lower("UserName")='sagdi')>1 THEN
  RAISE EXCEPTION 'Hay nombres duplicados o inconsistentes; no se modifica nada.';
 END IF;
 SELECT "Id" INTO cuenta_id FROM "AspNetUsers" WHERE "NormalizedUserName"='DEMO' OR lower("UserName")='demo';
 SELECT "Id" INTO destino_id FROM "AspNetUsers" WHERE "NormalizedUserName"='SAGDI' OR lower("UserName")='sagdi';
 IF cuenta_id IS NULL THEN
  IF destino_id IS NOT NULL AND EXISTS(SELECT 1 FROM "AspNetUsers" WHERE "Id"=destino_id AND lower("UserName")='sagdi' AND "NormalizedUserName"='SAGDI') AND EXISTS(SELECT 1 FROM "AuditLogs" WHERE "SourceKey"='rename-user:'||destino_id||':demo:sagdi') THEN RETURN; END IF;
  RAISE EXCEPTION 'No se encontró demo o sagdi ya existe sin evidencia de este renombre. Revisar sin crear otra cuenta.';
 END IF;
 IF NOT EXISTS(SELECT 1 FROM "AspNetUsers" WHERE "Id"=cuenta_id AND lower("UserName")='demo' AND "NormalizedUserName"='DEMO') THEN RAISE EXCEPTION 'El nombre y su normalización no coinciden; revisar antes de renombrar.'; END IF;
 IF destino_id IS NOT NULL THEN RAISE EXCEPTION 'Ya existe sagdi. No se fusiona ni reemplaza ninguna cuenta.'; END IF;
 SELECT to_jsonb(u)-ARRAY['UserName','NormalizedUserName','SecurityStamp','ConcurrencyStamp'] INTO antes FROM "AspNetUsers" u WHERE "Id"=cuenta_id;
 UPDATE "AspNetUsers" SET "UserName"='sagdi',"NormalizedUserName"='SAGDI',
 "SecurityStamp"=md5(random()::text||clock_timestamp()::text||cuenta_id),
 "ConcurrencyStamp"=md5(random()::text||clock_timestamp()::text||cuenta_id)
 WHERE "Id"=cuenta_id;
 SELECT to_jsonb(u)-ARRAY['UserName','NormalizedUserName','SecurityStamp','ConcurrencyStamp'] INTO despues FROM "AspNetUsers" u WHERE "Id"=cuenta_id;
 IF antes IS DISTINCT FROM despues THEN RAISE EXCEPTION 'Cambió un dato ajeno al renombre. Se revierte la operación.'; END IF;
 INSERT INTO "AuditLogs"("OccurredAt","Actor","Ip","Action","Target","Detail","Origin","SourceKey")
 VALUES(clock_timestamp(),'DB:'||current_user,coalesce(inet_client_addr()::text,'local'),
 'Renombre de usuario','CuentaId '||cuenta_id,'Nombre anterior: demo · Nombre actual: sagdi. Se conservan identificador, contraseña, perfiles, claims y bloqueo. Se renuevan las sesiones.','Configuración SQL','rename-user:'||cuenta_id||':demo:sagdi');
END $$;
COMMIT;
SELECT "Id","UserName","NormalizedUserName" FROM "AspNetUsers" WHERE lower("UserName") IN('demo','sagdi');
