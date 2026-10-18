"""Kullanim: tools/demo/reset-demo.ps1 (uusport_ui veritabanini sifirlar, API'yi 5081'de acar, bu betigi calistirir).
UU Sport deneme ortami (uusport_ui) icin zengin SENTETIK veri. Tum kisi/firma adlari uydurmadir.
Uc sirket, gecmis 3 ay satis/alis/fatura/tahsilat, kredi, kart, cek, planli kalemler ve grup ici islemler.
Tum yazmalar gercek HTTP uclarindan gecer (is kurallari ve RLS uygulanir)."""
import base64, hashlib, hmac, http.cookiejar, json, os, struct, sys, time, urllib.request, urllib.error
from datetime import date

BASE = 'http://127.0.0.1:5081'
# Iki adimli dogrulama anahtarlari repo disina yazilir (yalniz bu makine, sentetik kullanicilar).
OUT = os.path.join(os.environ['LOCALAPPDATA'], 'UUSport', 'demo')
os.makedirs(OUT, exist_ok=True)
PW = 'SifreTest12345678'  # yalniz yerel sentetik deneme veritabani


def totp(key):
    k = base64.b32decode(key.replace(' ', '').upper() + '=' * (-len(key.replace(' ', '')) % 8))
    h = hmac.new(k, struct.pack('>Q', int(time.time() // 30)), hashlib.sha1).digest()
    o = h[-1] & 15
    return '%06d' % ((struct.unpack('>I', h[o:o + 4])[0] & 0x7fffffff) % 1000000)


class Client:
    def __init__(self):
        self.op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()))

    def call(self, method, path, body=None, co=None):
        headers = {'Content-Type': 'application/json; charset=utf-8'}
        if method != 'GET':
            headers['X-UUSport-Request'] = '1'
        if co:
            headers['X-UUSport-Companies'] = co
        data = None if method == 'GET' else json.dumps(body if body is not None else {}).encode('utf-8')
        req = urllib.request.Request(BASE + path, data=data, method=method, headers=headers)
        try:
            with self.op.open(req) as r:
                t = r.read().decode('utf-8')
                return json.loads(t) if t else None
        except urllib.error.HTTPError as e:
            raise SystemExit(f'{method} {path} [{co}] -> {e.code} {e.read().decode("utf-8")}')

    def login_new(self, user, keyfile):
        self.call('POST', '/api/auth/login', {'userName': user, 'password': PW})
        key = self.call('GET', '/api/auth/mfa/setup')['sharedKey'].replace(' ', '')
        self.call('POST', '/api/auth/mfa/enable', {'code': totp(key)})
        open(os.path.join(OUT, keyfile), 'w').write(key)


S = Client()
S.call('GET', '/api/setup/status')
tok_path = os.path.join(os.environ['LOCALAPPDATA'], 'UUSport', 'secrets', 'setup-token.txt')
for _ in range(40):
    if os.path.exists(tok_path):
        break
    time.sleep(0.1)
token = open(tok_path).read().strip()
S.call('POST', '/api/setup', {'organizationName': 'UU Sport', 'userName': 'patron', 'fullName': 'Deneme Patron', 'password': PW, 'token': token, 'companyCode': 'UUS'})
S.login_new('patron', 'patron.key')
S.call('POST', '/api/companies', {'name': 'Fitness', 'code': 'FIT', 'sortOrder': 1})
S.call('POST', '/api/companies', {'name': 'Tiyatro', 'code': 'TIY', 'sortOrder': 2})
CO = {c['code']: c['id'] for c in S.call('GET', '/api/companies/all')}
print('sirketler', CO)


# ---------------- yardimcilar ----------------
def party(co, name, kind):
    return S.call('POST', '/api/parties', {'name': name, 'kind': kind}, CO[co])['id']


def item(co, name, kind, price, vat, incl=True):
    return S.call('POST', '/api/items', {'name': name, 'kind': kind, 'unitPrice': str(price), 'priceIncludesVat': incl, 'vatRate': str(vat), 'unit': 'adet'}, CO[co])['id']


def accounts(co):
    return {a['name']: a['id'] for a in S.call('GET', '/api/cash-accounts', None, CO[co])}


def line(co, p, it, direction, d, qty=1, price=None):
    r = S.call('POST', '/api/trade-lines', {'partyId': p, 'itemId': it, 'direction': direction, 'occurredOn': d, 'quantity': str(qty),
                                           'unitPrice': None if price is None else str(price), 'discountPercent': '0'}, CO[co])
    return r['id'], float(r['grossAmount'])


DOC_NO = {}


def document(co, p, direction, d, line_ids, no=None):
    rows = [l for l in S.call('GET', f'/api/parties/{p}/lines', None, CO[co]) if l['id'] in line_ids]
    vat = {}
    for l in rows:
        v = vat.setdefault(l['vatRate'], [0.0, 0.0])
        v[0] += float(l['netAmount']); v[1] += float(l['vatAmount'])
    gross = round(sum(float(l['grossAmount']) for l in rows), 2)
    if no is None:
        DOC_NO[co] = DOC_NO.get(co, 0) + 1
        no = f'{co}2026{DOC_NO[co]:06d}'
    S.call('POST', '/api/documents', {
        'partyId': p, 'direction': direction, 'type': 'e_arsiv', 'documentNo': no, 'documentDate': d, 'grossTotal': f'{gross:.2f}',
        'vatLines': [{'vatRate': r, 'netAmount': f'{n:.2f}', 'vatAmount': f'{x:.2f}'} for r, (n, x) in vat.items()],
        'links': [{'tradeLineId': l['id'], 'quantity': l['quantity']} for l in rows], 'note': None}, CO[co])
    return no


def pay(co, p, direction, method, acc, amount, d, alloc=None, desc=None):
    S.call('POST', '/api/payments', {'partyId': p, 'direction': direction, 'method': method, 'cashAccountId': acc, 'amount': f'{amount:.2f}',
                                    'occurredOn': d, 'reference': None, 'description': desc,
                                    'allocations': [{'tradeLineId': a, 'amount': f'{m:.2f}'} for a, m in (alloc or [])]}, CO[co])


def planned(co, kind, name, amount, day, start, interval=1):
    S.call('POST', '/api/planned-cash-items', {'kind': kind, 'name': name, 'amount': str(amount), 'dayOfMonth': day, 'intervalMonths': interval,
                                              'startMonth': start, 'endMonth': None}, CO[co])


def card_account(co, due_day):
    acc = accounts(co)
    return S.call('POST', '/api/cash-accounts', {'name': 'Şirket Kredi Kartı', 'kind': 'credit_card', 'parentAccountId': acc['Banka'], 'paymentDueDay': due_day}, CO[co])['id']


def loan(co, lender, name, principal, n, first_due, interest):
    acc = accounts(co)
    loan_acc = S.call('POST', '/api/cash-accounts', {'name': name, 'kind': 'loan', 'parentAccountId': acc['Banka']}, CO[co])['id']
    y, m, d = map(int, first_due.split('-'))
    inst = []
    for i in range(n):
        mm = m + i
        inst.append({'installmentNo': i + 1, 'dueDate': f'{y + (mm - 1) // 12}-{(mm - 1) % 12 + 1:02d}-{d:02d}',
                     'principalAmount': f'{principal / n:.2f}', 'interestAmount': f'{interest:.2f}', 'feeAmount': '0'})
    S.call('POST', '/api/loans', {'partyId': lender, 'cashAccountId': loan_acc, 'name': name, 'principalAmount': f'{principal:.2f}',
                                 'startDate': '2026-10-01', 'notes': 'sentetik', 'installments': inst}, CO[co])


def checks(co, p, direction, rows):
    S.call('POST', '/api/checks', {'partyId': p, 'direction': direction, 'bankName': 'Deneme Bankası',
                                  'checks': [{'checkNo': no, 'dueDate': d, 'amount': f'{a:.2f}'} for no, d, a in rows]}, CO[co])


MONTHS = ['2026-08', '2026-09', '2026-10']


def capital(co, amount):
    """Ortagin sirkete koydugu nakit (sentetik baslangic sermayesi), banka hesabina."""
    o = party(co, 'Ortak Cari Hesabı (deneme)', 'both')
    pay(co, o, 'receipt', 'transfer', accounts(co)['Banka'], amount, '2026-08-01', None, 'Ortak sermaye avansı')
METHODS = [('transfer', 'Banka'), ('cash', 'Merkez Kasa'), ('card', 'POS')]


def monthly_sales(co, customers, service, invoice_until, unpaid):
    """Her musteriye her ay hizmet satisi; belirli aya kadar fatura, odenmeyenler haric tahsilat + dagitim."""
    acc = accounts(co)
    for mi, m in enumerate(MONTHS):
        for ci, c in enumerate(customers):
            lid, gross = line(co, c, service, 'sale', f'{m}-01')
            if mi <= invoice_until or ci % 2 == 0:
                document(co, c, 'sale', f'{m}-03', [lid])
            if (m, ci) in unpaid:
                continue
            method, an = METHODS[(ci + mi) % 3]
            pay(co, c, 'receipt', method, acc[an], gross, f'{m}-{5 + ci:02d}', [(lid, gross)])


# ---------------- UUS: futbol akademisi ----------------
capital('UUS', 80000)
uus_c = [party('UUS', n, 'customer') for n in ['Deniz Aksoy (veli)', 'Ece Karaca (veli)', 'Baran Öztürk (veli)', 'Selin Uçar (veli)',
                                                'Kaan Erdem (veli)', 'Melis Sarı (veli)', 'Onur Tekin (veli)', 'Zehra Polat (veli)']]
uus_aidat = item('UUS', 'Futbol Akademisi Aylık Aidat', 'service', 2500, 10)
uus_kamp = item('UUS', 'Yaz/Kış Kampı', 'service', 7500, 10)
uus_forma = item('UUS', 'Forma Seti', 'stock', 1200, 20)
uus_sup = party('UUS', 'Anadolu Spor Malzemeleri Ltd. (deneme)', 'supplier')
uus_clean = party('UUS', 'Parlak Temizlik Hizmetleri (deneme)', 'supplier')
uus_fuel = party('UUS', 'Yol Akaryakıt (deneme)', 'supplier')
uus_cleaning = item('UUS', 'Temizlik Hizmeti', 'service', 6000, 20, incl=False)
uus_fuel_item = item('UUS', 'Akaryakıt', 'service', 1, 20)
acc = accounts('UUS')
# forma alisi + alis faturasi + odeme
lid, g = line('UUS', uus_sup, uus_forma, 'purchase', '2026-08-10', 20, 700)
document('UUS', uus_sup, 'purchase', '2026-08-10', [lid], 'ANS2026000418')
pay('UUS', uus_sup, 'disbursement', 'transfer', acc['Banka'], g, '2026-08-20', [(lid, g)])
monthly_sales('UUS', uus_c, uus_aidat, 1, {('2026-10', 1), ('2026-10', 4), ('2026-10', 6), ('2026-09', 6)})
for ci in (0, 2, 5):
    lid, g = line('UUS', uus_c[ci], uus_forma, 'sale', '2026-09-12')
    document('UUS', uus_c[ci], 'sale', '2026-09-12', [lid])
    pay('UUS', uus_c[ci], 'receipt', 'cash', acc['Merkez Kasa'], g, '2026-09-12', [(lid, g)])
for ci in (3, 7):
    line('UUS', uus_c[ci], uus_kamp, 'sale', '2026-10-14')  # kamp: fatura ve tahsilat bekliyor
for m in MONTHS:
    lid, g = line('UUS', uus_clean, uus_cleaning, 'purchase', f'{m}-28' if m != '2026-10' else '2026-10-15')
    document('UUS', uus_clean, 'purchase', f'{m}-28' if m != '2026-10' else '2026-10-15', [lid], f'PRL{m.replace("-", "")}01')
    if m != '2026-10':
        pay('UUS', uus_clean, 'disbursement', 'transfer', acc['Banka'], g, f'{m}-28', [(lid, g)])
uus_card = card_account('UUS', 15)
for d, amt in (('2026-09-18', 3500), ('2026-10-09', 4200)):
    lid, g = line('UUS', uus_fuel, uus_fuel_item, 'purchase', d, 1, amt)
    pay('UUS', uus_fuel, 'disbursement', 'card', uus_card, g, d, [(lid, g)])
uus_bank = party('UUS', 'Deneme Katılım Bankası', 'supplier')
loan('UUS', uus_bank, 'İşletme Kredisi', 60000, 6, '2026-10-25', 1200)
checks('UUS', uus_c[1], 'received', [('A-1001', '2026-11-20', 3000), ('A-1002', '2026-12-20', 2500)])
checks('UUS', uus_sup, 'issued', [('U-5001', '2026-11-30', 12000)])
planned('UUS', 'salary', 'Antrenör ve ofis maaşları', 55000, 1, '2026-11-01')
planned('UUS', 'sgk', 'SGK primi', 20000, 26, '2026-10-01')
planned('UUS', 'withholding_tax', 'Muhtasar', 6000, 26, '2026-10-01')
planned('UUS', 'rent', 'Tesis kirası', 18000, 5, '2026-11-01')
planned('UUS', 'provisional_tax', 'Geçici vergi', 15000, 17, '2026-11-01', 3)
planned('UUS', 'vat', 'KDV tahmini', 4000, 28, '2026-12-01')
planned('UUS', 'other_expense', 'Elektrik, su, doğalgaz', 4500, 20, '2026-10-01')
planned('UUS', 'other_income', 'Beklenen aidat tahsilatı (tüm öğrenciler)', 130000, 10, '2026-11-01')
print('UUS tamam')

# ---------------- FIT: spor salonu ----------------
capital('FIT', 60000)
fit_c = [party('FIT', n, 'customer') for n in ['Arda Yıldız (üye)', 'Buse Kılıç (üye)', 'Cem Aslan (üye)', 'Duygu Şen (üye)',
                                               'Emre Koç (üye)', 'Figen Ay (üye)', 'Gökhan Ural (üye)', 'Hande Bulut (üye)']]
fit_uyelik = item('FIT', 'Aylık Üyelik', 'service', 1800, 20)
fit_pt = item('FIT', 'Kişisel Antrenman 10 Ders', 'service', 6000, 20)
fit_sup = party('FIT', 'Fitness Ekipman A.Ş. (deneme)', 'supplier')
fit_elec = party('FIT', 'Elektrik Dağıtım (deneme)', 'supplier')
fit_elec_item = item('FIT', 'Elektrik Faturası', 'service', 1, 20)
fit_equip = item('FIT', 'Kondisyon Ekipmanı', 'stock', 45000, 20, incl=False)
acc = accounts('FIT')
monthly_sales('FIT', fit_c, fit_uyelik, 1, {('2026-10', 2), ('2026-10', 5)})
for ci in (1, 4):
    lid, g = line('FIT', fit_c[ci], fit_pt, 'sale', '2026-10-02')
    document('FIT', fit_c[ci], 'sale', '2026-10-02', [lid])
    pay('FIT', fit_c[ci], 'receipt', 'card', acc['POS'], g, '2026-10-02', [(lid, g)])
lid, g = line('FIT', fit_sup, fit_equip, 'purchase', '2026-09-05', 1)
document('FIT', fit_sup, 'purchase', '2026-09-05', [lid], 'FEK2026000912')
pay('FIT', fit_sup, 'disbursement', 'transfer', acc['Banka'], 39000, '2026-09-05', [(lid, 39000)])
for m, amt in (('2026-08', 7800), ('2026-09', 8400), ('2026-10', 9100)):
    lid, g = line('FIT', fit_elec, fit_elec_item, 'purchase', f'{m}-12', 1, amt)
    document('FIT', fit_elec, 'purchase', f'{m}-12', [lid], f'ELK{m.replace("-", "")}')
    pay('FIT', fit_elec, 'disbursement', 'transfer', acc['Banka'], g, f'{m}-20', [(lid, g)])
fit_card = card_account('FIT', 5)
fit_misc = party('FIT', 'Market Alışverişi (deneme)', 'supplier')
fit_misc_item = item('FIT', 'Sarf Malzeme', 'service', 1, 20)
lid, g = line('FIT', fit_misc, fit_misc_item, 'purchase', '2026-10-11', 1, 6000)
pay('FIT', fit_misc, 'disbursement', 'card', fit_card, g, '2026-10-11', [(lid, g)])
fit_bank = party('FIT', 'Deneme Kalkınma Bankası', 'supplier')
loan('FIT', fit_bank, 'Ekipman Kredisi', 36000, 12, '2026-11-10', 400)
checks('FIT', fit_sup, 'issued', [('F-2001', '2026-11-15', 15000)])
checks('FIT', fit_c[0], 'received', [('F-3001', '2027-01-10', 4000)])
planned('FIT', 'salary', 'Eğitmen maaşları', 35000, 1, '2026-11-01')
planned('FIT', 'sgk', 'SGK primi', 13000, 26, '2026-10-01')
planned('FIT', 'withholding_tax', 'Muhtasar', 3500, 26, '2026-10-01')
planned('FIT', 'rent', 'Salon kirası', 25000, 3, '2026-11-01')
planned('FIT', 'provisional_tax', 'Geçici vergi', 8000, 17, '2026-11-01', 3)
planned('FIT', 'other_income', 'Beklenen üyelik tahsilatı (tüm üyeler)', 92000, 10, '2026-11-01')
print('FIT tamam')

# ---------------- TIY: tiyatro ----------------
capital('TIY', 10000)
tiy_c = [party('TIY', n, 'customer') for n in ['İlker Demirci (seyirci)', 'Jale Uysal (atölye)', 'Kerem Bal (atölye)',
                                               'Leyla Güneş (atölye)', 'Mert Oral (seyirci)', 'Nil Çetin (atölye)']]
tiy_atolye = item('TIY', 'Tiyatro Atölyesi Aylık', 'service', 2000, 10)
tiy_bilet = item('TIY', 'Oyun Bileti', 'service', 400, 20)
tiy_salon = item('TIY', 'Salon Kiralama', 'service', 8000, 20, incl=False)
tiy_sup = party('TIY', 'Dekor ve Kostüm Atölyesi (deneme)', 'supplier')
tiy_dekor = item('TIY', 'Dekor Yapımı', 'service', 14000, 20, incl=False)
acc = accounts('TIY')
monthly_sales('TIY', tiy_c[1:4] + tiy_c[5:], tiy_atolye, 1, {('2026-10', 0)})
for ci, qty in ((0, 4), (4, 2)):
    lid, g = line('TIY', tiy_c[ci], tiy_bilet, 'sale', '2026-10-04', qty)
    document('TIY', tiy_c[ci], 'sale', '2026-10-04', [lid])
    pay('TIY', tiy_c[ci], 'receipt', 'card', acc['POS'], g, '2026-10-04', [(lid, g)])
lid, g = line('TIY', tiy_sup, tiy_dekor, 'purchase', '2026-09-22')
document('TIY', tiy_sup, 'purchase', '2026-09-22', [lid], 'DKR2026000077')
pay('TIY', tiy_sup, 'disbursement', 'transfer', acc['Banka'], 8000, '2026-09-30', [(lid, 8000)])
checks('TIY', tiy_c[0], 'received', [('T-4001', '2026-10-08', 700)])  # vadesi gecmis
planned('TIY', 'salary', 'Oyuncu ve teknik ekip', 16000, 1, '2026-11-01')
planned('TIY', 'sgk', 'SGK primi', 6000, 26, '2026-10-01')
planned('TIY', 'withholding_tax', 'Muhtasar', 1800, 26, '2026-10-01')
planned('TIY', 'rent', 'Sahne kirası', 9000, 5, '2026-11-01')
planned('TIY', 'other_income', 'Beklenen bilet ve atölye geliri', 36000, 15, '2026-11-01')
print('TIY tamam')

# ---------------- grup ici islemler (karsilikli elle; onay kuyrugu ADR-012 2.5 henuz yok) ----------------
uus_fit = party('UUS', 'FIT · Fitness (grup içi)', 'customer')
fit_uus = party('FIT', 'UUS · UU Sport (grup içi)', 'supplier')
uus_tesis = item('UUS', 'Tesis Kullanım Bedeli (grup içi)', 'service', 15000, 20, incl=False)
fit_tesis = item('FIT', 'Tesis Kullanım Bedeli (UUS)', 'service', 15000, 20, incl=False)
lid, g = line('UUS', uus_fit, uus_tesis, 'sale', '2026-09-30')
no = document('UUS', uus_fit, 'sale', '2026-09-30', [lid])
lid2, g2 = line('FIT', fit_uus, fit_tesis, 'purchase', '2026-09-30')
document('FIT', fit_uus, 'purchase', '2026-09-30', [lid2], no)
pay('FIT', fit_uus, 'disbursement', 'transfer', accounts('FIT')['Banka'], 10000, '2026-10-10', [(lid2, 10000)], 'Grup içi: tesis bedeli kısmi ödeme')
pay('UUS', uus_fit, 'receipt', 'transfer', accounts('UUS')['Banka'], 10000, '2026-10-10', [(lid, 10000)], 'Grup içi: FIT tesis bedeli kısmi tahsilat')

tiy_uus = party('TIY', 'UUS · UU Sport (grup içi)', 'customer')
uus_tiy = party('UUS', 'TIY · Tiyatro (grup içi)', 'supplier')
uus_salon = item('UUS', 'Salon Kiralama (TIY)', 'service', 8000, 20, incl=False)
lid, g = line('TIY', tiy_uus, tiy_salon, 'sale', '2026-10-06')
no = document('TIY', tiy_uus, 'sale', '2026-10-06', [lid])
lid2, g2 = line('UUS', uus_tiy, uus_salon, 'purchase', '2026-10-06')
document('UUS', uus_tiy, 'purchase', '2026-10-06', [lid2], no)
pay('UUS', uus_tiy, 'disbursement', 'transfer', accounts('UUS')['Banka'], g2, '2026-10-12', [(lid2, g2)], 'Grup içi: salon kirası')
pay('TIY', tiy_uus, 'receipt', 'transfer', accounts('TIY')['Banka'], g, '2026-10-12', [(lid, g)], 'Grup içi: UUS salon kirası')

fit_tiy = party('FIT', 'TIY · Tiyatro (grup içi)', 'both')
tiy_fit = party('TIY', 'FIT · Fitness (grup içi)', 'both')
pay('FIT', fit_tiy, 'disbursement', 'transfer', accounts('FIT')['Banka'], 5000, '2026-10-15', None, 'Grup içi geçici nakit desteği (borç)')
pay('TIY', tiy_fit, 'receipt', 'transfer', accounts('TIY')['Banka'], 5000, '2026-10-15', None, 'Grup içi geçici nakit desteği (FIT\'e borç)')
print('grup ici tamam')

# ---------------- kullanicilar ----------------
S.call('POST', '/api/users', {'userName': 'muhasebe-ab', 'fullName': 'Deneme Muhasebe UUS-FIT', 'password': PW, 'maxDiscountPercent': '10',
                              'memberships': [{'tenantId': CO['UUS'], 'role': 'Muhasebe'}, {'tenantId': CO['FIT'], 'role': 'Muhasebe'}]})
S.call('POST', '/api/users', {'userName': 'muhasebe-c', 'fullName': 'Deneme Muhasebe TIY', 'password': PW, 'maxDiscountPercent': '10',
                              'memberships': [{'tenantId': CO['TIY'], 'role': 'Muhasebe'}]})
S.call('POST', '/api/users', {'userName': 'ortak-b', 'fullName': 'Deneme Ortak FIT', 'password': PW, 'maxDiscountPercent': '0',
                              'memberships': [{'tenantId': CO['FIT'], 'role': 'Ortak', 'grantedKeys': ['ledger.read', 'cash.read', 'reports.detail']}]})
for u in ('muhasebe-ab', 'muhasebe-c', 'ortak-b'):
    Client().login_new(u, f'{u}.key')
print('kullanicilar tamam')
