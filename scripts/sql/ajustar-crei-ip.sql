-- Ejecutar sólo durante el paso acordado, con respaldo previo y en la base correcta.
-- Conserva comedores, horarios, cuentas y consumos. Sólo sustituye terminales de CREI.
\set ON_ERROR_STOP on
BEGIN;
SELECT pg_advisory_xact_lock(7411,3);
DO $$
DECLARE room_id integer; previous_ips text;
BEGIN
 IF to_regclass('public."AuditLogs"') IS NULL THEN RAISE EXCEPTION 'Primero instalar la versión con auditoría.'; END IF;
 IF (SELECT count(*) FROM "DiningRooms" WHERE upper(trim("Name"))='CREI')<>1 THEN RAISE EXCEPTION 'Debe existir exactamente un comedor CREI.'; END IF;
 SELECT "Id" INTO room_id FROM "DiningRooms" WHERE upper(trim("Name"))='CREI';
 IF EXISTS(SELECT 1 FROM "Terminals" WHERE "Ip"='172.16.70.68' AND "DiningRoomId"<>room_id) THEN RAISE EXCEPTION 'La IP ya está asignada a otro comedor; no se modifica nada.'; END IF;
 SELECT string_agg("Ip",', ' ORDER BY "Ip") INTO previous_ips FROM "Terminals" WHERE "DiningRoomId"=room_id;
 IF (SELECT count(*) FROM "Terminals" WHERE "DiningRoomId"=room_id)=1 AND EXISTS(SELECT 1 FROM "Terminals" WHERE "DiningRoomId"=room_id AND "Ip"='172.16.70.68') THEN RETURN; END IF;
 DELETE FROM "Terminals" WHERE "DiningRoomId"=room_id AND "Ip"<>'172.16.70.68';
 INSERT INTO "Terminals"("Ip","DiningRoomId") SELECT '172.16.70.68',room_id WHERE NOT EXISTS(SELECT 1 FROM "Terminals" WHERE "Ip"='172.16.70.68');
 INSERT INTO "AuditLogs"("OccurredAt","Actor","Ip","Action","Target","Detail","Origin","SourceKey")
 VALUES(clock_timestamp(),'DB:'||current_user,coalesce(inet_client_addr()::text,'local'),'Cambio de terminal CREI','Comedor CREI','IPs anteriores: '||coalesce(previous_ips,'ninguna')||' · IP autorizada: 172.16.70.68','Configuración SQL',NULL);
END $$;
COMMIT;
SELECT r."Name",t."Ip" FROM "DiningRooms" r LEFT JOIN "Terminals" t ON t."DiningRoomId"=r."Id" ORDER BY r."Name",t."Ip";
