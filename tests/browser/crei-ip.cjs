// Prueba SQL aislada; no se conecta al servidor 172.16.2.12.
const assert=require('node:assert/strict'),fs=require('node:fs'),{execFileSync}=require('node:child_process');
const name='comedores_crei_config_test',docker=['--host=unix:///var/run/docker.sock','exec','-i','comedores-postgres'];
const run=(command,args=[],input)=>execFileSync('docker',[...docker,command,...args],{encoding:'utf8',input,stdio:['pipe','pipe','pipe']});
const sql=q=>run('psql',['-U','comedores','-d',name,'-At','-v','ON_ERROR_STOP=1','-c',q]).trim();
const script=fs.readFileSync('scripts/sql/ajustar-crei-ip.sql','utf8');
run('createdb',['-U','comedores',name]);
try{
 sql(`CREATE TABLE "DiningRooms"("Id" int PRIMARY KEY,"Name" text);CREATE TABLE "Terminals"("Id" int GENERATED ALWAYS AS IDENTITY PRIMARY KEY,"Ip" text UNIQUE,"DiningRoomId" int REFERENCES "DiningRooms"("Id"));CREATE TABLE "AuditLogs"("Id" bigint GENERATED ALWAYS AS IDENTITY,"OccurredAt" timestamptz,"Actor" text,"Ip" text,"Action" text,"Target" text,"Detail" text,"Origin" text,"SourceKey" text);INSERT INTO "DiningRooms" VALUES(1,'SUBESTACION'),(2,'PLATAFORMA'),(3,'CREI');INSERT INTO "Terminals"("Ip","DiningRoomId") VALUES('172.16.66.230',1),('172.16.60.54',2),('172.16.70.68',3),('172.18.208.1',3),('172.18.219.13',3);`);
 const others=sql(`SELECT row_to_json(t) FROM "Terminals" t WHERE "DiningRoomId"<>3 ORDER BY "Id"`),rooms=sql('TABLE "DiningRooms"');
 const apply=()=>run('psql',['-U','comedores','-d',name,'-f','-'],script);
 apply();assert.equal(sql(`SELECT string_agg("Ip",',') FROM "Terminals" WHERE "DiningRoomId"=3`),'172.16.70.68');assert.equal(sql(`SELECT row_to_json(t) FROM "Terminals" t WHERE "DiningRoomId"<>3 ORDER BY "Id"`),others);assert.equal(sql('TABLE "DiningRooms"'),rooms);assert.equal(sql('SELECT count(*) FROM "AuditLogs"'),'1');assert(sql('SELECT "Detail" FROM "AuditLogs"').includes('172.18.208.1'));assert.equal(sql('SELECT "Actor" FROM "AuditLogs"'),'DB:comedores');
 apply();assert.equal(sql('SELECT count(*) FROM "AuditLogs"'),'1');
 sql(`DELETE FROM "Terminals" WHERE "DiningRoomId"=3;UPDATE "Terminals" SET "Ip"='172.16.70.68' WHERE "DiningRoomId"=1;`);const before=sql('TABLE "Terminals"');assert.throws(apply);assert.equal(sql('TABLE "Terminals"'),before);assert.equal(sql('SELECT count(*) FROM "AuditLogs"'),'1');
 console.log('PASS: CREI queda sólo con 172.16.70.68; otros comedores y terminales intactos; auditoría SQL e idempotencia; conflicto con otro comedor revierte sin cambios.');
}finally{run('dropdb',['-U','comedores','--force',name]);}
