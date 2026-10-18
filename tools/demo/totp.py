import sys,hmac,hashlib,struct,time,base64
key=sys.argv[1].replace(" ","").upper()
k=base64.b32decode(key+"="*(-len(key)%8))
c=int(time.time()//30)
h=hmac.new(k,struct.pack(">Q",c),hashlib.sha1).digest();o=h[-1]&15
print("%06d"%((struct.unpack(">I",h[o:o+4])[0]&0x7fffffff)%1000000))
