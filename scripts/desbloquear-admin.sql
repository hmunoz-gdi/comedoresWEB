-- Recuperación local de emergencia: ejecutar en pgAdmin sobre la base comedores.
-- Sólo desbloquea demo. No cambia contraseñas, perfiles, empleados ni consumos.
BEGIN;
WITH restored AS (
 UPDATE "AspNetUsers"
 SET "AccessFailedCount"=0,"LockoutEnd"=NULL,"SecurityStamp"=gen_random_uuid()::text
 WHERE "NormalizedUserName"='DEMO' AND "LockoutEnd">CURRENT_TIMESTAMP
 RETURNING "UserName"
)
INSERT INTO "AccountSecurityEvents" ("OccurredAt","Account","Actor","Action","Reason")
SELECT CURRENT_TIMESTAMP,"UserName",'Administrador local de PostgreSQL','Desbloqueo',
 'Recuperación local de emergencia de la cuenta demo.' FROM restored;
COMMIT;
SELECT "UserName","LockoutEnd","AccessFailedCount" FROM "AspNetUsers" WHERE "NormalizedUserName"='DEMO';
