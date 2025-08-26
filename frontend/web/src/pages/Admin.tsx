import { useEffect, useState } from 'react'; import axios from 'axios'
type AssetType = { id:number, name:string }
type FieldDef = { id:number, assetTypeId:number, name:string, dataType:string, required:boolean, ownerDepartment?:string, privacyLevel:string, optionsCsv?:string }
export default function Admin(){
  const [types,setTypes]=useState<AssetType[]>([]); const [newType,setNewType]=useState(''); const [typeId,setTypeId]=useState<number|undefined>();
  const [fields,setFields]=useState<FieldDef[]>([]);
  async function loadTypes(){ const r=await axios.get('/api/asset-types'); setTypes(r.data); }
  async function createType(){ if(!newType.trim())return; await axios.post('/api/asset-types',{name:newType}); setNewType(''); loadTypes(); }
  async function loadFields(){ if(!typeId)return; const r=await axios.get(`/api/fields/by-type/${typeId}`); setFields(r.data); }
  useEffect(()=>{loadTypes()},[]); useEffect(()=>{loadFields()},[typeId]);
  const [n,setN]=useState(''); const [dt,setDt]=useState('Text'); const [req,setReq]=useState(false); const [own,setOwn]=useState(''); const [pl,setPl]=useState('Public'); const [opt,setOpt]=useState('')
  async function addField(){ if(!typeId||!n.trim())return; await axios.post('/api/fields',{assetTypeId:typeId,name:n,dataType:dt,required:req,ownerDepartment:own||null,privacyLevel:pl,optionsCsv:opt||null}); setN('');setReq(false);setOwn('');setPl('Public');setDt('Text');setOpt(''); loadFields() }
  return (<div><h3>Admin</h3>
    <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:16}}>
      <div><h4>Asset Types</h4><div style={{display:'flex',gap:8}}><input placeholder="New type" value={newType} onChange={e=>setNewType(e.target.value)}/><button onClick={createType}>Add</button></div>
        <ul>{types.map(t=>(<li key={t.id}><button onClick={()=>setTypeId(t.id)}>use</button> #{t.id} {t.name}</li>))}</ul></div>
      <div><h4>Fields {typeId?`(Type #${typeId})`:''}</h4>
        {typeId&&(<div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:8,marginBottom:12}}>
          <input placeholder="Field name" value={n} onChange={e=>setN(e.target.value)}/>
          <select value={dt} onChange={e=>setDt(e.target.value)}><option>Text</option><option>Number</option><option>Date</option><option>Dropdown</option><option>Attachment</option><option>Location</option></select>
          <input placeholder="Owner dept" value={own} onChange={e=>setOwn(e.target.value)}/>
          <select value={pl} onChange={e=>setPl(e.target.value)}><option>Public</option><option>Confidential</option><option>Restricted</option></select>
          <label><input type="checkbox" checked={req} onChange={e=>setReq(e.target.checked)}/> Required</label>
          <input placeholder="Options csv" value={opt} onChange={e=>setOpt(e.target.value)}/>
          <button onClick={addField}>Add Field</button></div>)}
        <table border={1} cellPadding={6}><thead><tr><th>ID</th><th>Name</th><th>Type</th><th>Req</th><th>Owner</th><th>Privacy</th></tr></thead>
          <tbody>{fields.map(f=>(<tr key={f.id}><td>{f.id}</td><td>{f.name}</td><td>{f.dataType}</td><td>{f.required?'Yes':'No'}</td><td>{f.ownerDepartment||'-'}</td><td>{f.privacyLevel}</td></tr>))}</tbody></table>
      </div>
    </div></div>)
}
