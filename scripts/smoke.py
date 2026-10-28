"""Comprueba el flujo HTTP contra el servidor de desarrollo con datos ficticios."""
import html, http.cookiejar, re, urllib.parse, urllib.request, json, os
from pathlib import Path
base=os.environ.get('TEST_BASE_URL','http://127.0.0.1:5080')
opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))
def get(path):
 with opener.open(base+path) as response: return html.unescape(response.read().decode())
def post(path,values):
 with opener.open(base+path,urllib.parse.urlencode(values).encode()) as response: return html.unescape(response.read().decode())
def token(page):
 return html.unescape(re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',page).group(1))
page=get('/Reports')
assert 'Iniciar sesión' in page, 'Los reportes deben exigir autenticación'
page=post('/Login',{'Username':'demo','Password':(Path(__file__).parent.parent/'.local/demo-password').read_text().strip(),'__RequestVerificationToken':token(page)})
assert 'Registrar servicio' in page
page=get('/Register?Number=40001')
assert 'Empleado de demostración' in page
request=re.search(r'id="RequestId"[^>]*value="([^"]+)"',page).group(1)
fields={'Number':'40001','RequestId':request,'__RequestVerificationToken':token(page)}
result=json.loads(post('/Register?handler=Quick',fields))
while result.get('needsConfirmation'):
 fields['ConfirmedSegmentCount']=result['segmentCount']
 fields['ConfirmedServiceType']=result['serviceType']
 result=json.loads(post('/Register?handler=Quick',fields))
assert result['ok'] and 'Servicio registrado.' in result['message']
result=json.loads(post('/Register?handler=Quick',fields))
assert result['ok'] and 'no se duplic' in result['message']
page=get('/Reports?From=2026-10-19&To=2026-10-19&Category=Operativo')
assert '<td>40001</td>' in page and 'SUBESTACION' in page
page=get('/Reports?From=2026-10-19&To=2026-10-19&Category=Mando')
assert '<td>40001</td>' not in page
page=get('/Reports?From=2026-10-20&To=2026-10-19')
assert 'La fecha inicial' in page
print('PASS: acceso protegido, login, consulta, registro, reenvío, reporte, filtro de categoría y rango inválido.')
