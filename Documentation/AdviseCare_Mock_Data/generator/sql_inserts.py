import re
def split_values(s, i):
    """parse '(v, v, ...)' starting at s[i]=='(' ; return (list, new_i)"""
    assert s[i]=='('
    i+=1; vals=[]; n=len(s)
    while True:
        while s[i] in ' \t\r\n': i+=1
        if s[i]==')': return vals, i+1
        if s[i]=='N' and s[i+1]=="'": i+=1; kind='n'
        else: kind='s'
        if s[i]=="'":
            j=i+1; buf=[]
            while True:
                k=s.index("'",j)
                buf.append(s[j:k])
                if k+1<n and s[k+1]=="'": buf.append("'"); j=k+2; continue
                j=k+1; break
            v=''.join(buf); i=j
            vals.append(('str' if kind=='n' else 'lit', v))
        else:
            m=re.compile(r"NULL|0x[0-9A-Fa-f]*|-?\d+(?:\.\d+)?").match(s,i)
            tok=m.group(0); i=m.end()
            if tok=='NULL': vals.append(None)
            elif tok.startswith('0x'): vals.append(('bin',tok))
            else: vals.append(('num',tok))
        while s[i] in ' \t\r\n': i+=1
        if s[i]==',': i+=1
def parse_inserts(text):
    data={}; order=[]
    for m in re.finditer(r"INSERT INTO \[dbo\]\.\[(\w+)\] \(([^)]*)\) VALUES", text):
        t=m.group(1); cols=[c.strip()[1:-1] for c in m.group(2).split(',')]
        i=m.end()
        if t not in data: data[t]={'cols':cols,'rows':[]}; order.append(t)
        assert data[t]['cols']==cols, t
        while True:
            while text[i] in ' \t\r\n': i+=1
            row,i=split_values(text,i)
            assert len(row)==len(cols),(t,row)
            data[t]['rows'].append(dict(zip(cols,row)))
            while text[i] in ' \t\r\n': i+=1
            if text[i]==',': i+=1; continue
            if text[i]==';': break
            raise Exception(text[i-50:i+50])
    return data, order
