-- Solo para la base temporal de pruebas. Los cuatro consumos pertenecen a empleados ficticios.
INSERT INTO "Consumptions" ("RequestId","EmployeeId","DiningRoomId","OccurredAt","ServiceDate","Type","SpecialShift","EmployeeName","Position","Ip","Operator","DailySequence")
SELECT gen_random_uuid(),e."Id",1,v.occurred,'2026-10-20'::date,v.type,false,'Empleado de demostración','Operativo','127.0.0.1','prueba',v.seq
FROM "Employees" e CROSS JOIN (VALUES
 ('2026-10-20T14:05:11Z'::timestamptz,'DYN',1),
 ('2026-10-20T20:10:12Z'::timestamptz,'COM',2),
 ('2026-10-20T20:20:13Z'::timestamptz,'COM',3),
 ('2026-10-21T02:35:14Z'::timestamptz,'CEN',4)
) v(occurred,type,seq) WHERE e."Number"='40001';
