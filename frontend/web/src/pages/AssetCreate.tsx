import { useEffect, useState } from 'react'
import axios from 'axios'
import L from 'leaflet'

type AssetType = { id:number, name:string }
type FieldDef = { id:number, name:string, dataType:'Text'|'Number'|'Date'|'Dropdown'|'Attachment'|'Location', required:boolean, optionsCsv?:string, privacyLevel:string }

export default function AssetCreate() {
  const [types, setTypes] = useState<AssetType[]>([])
  const [assetTypeId, setAssetTypeId] = useState<number|undefined>()
  const [fields, setFields] = useState<FieldDef[]>([])
  const [name, setName] = useState('')
  const [region, setRegion] = useState('Riyadh')
  const [city, setCity] = useState('Riyadh')
  const [lat, setLat] = useState<number|undefined>(undefined)
  const [lng, setLng] = useState<number|undefined>(undefined)
  const [values, setValues] = useState<Record<string,string>>({})
  const [attachments, setAttachments] = useState<Record<string,File|null>>({})

  async function loadTypes(){ const r = await axios.get('/api/asset-types'); setTypes(r.data) }
  async function loadFields(){ if(!assetTypeId) return; const r = await axios.get(`/api/fields/by-type/${assetTypeId}`); setFields(r.data) }
  useEffect(()=>{ loadTypes() },[])
  useEffect(()=>{ loadFields() },[assetTypeId])

  useEffect(()=>{ const map = L.map('map').setView([24.7136,46.6753],6)
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',{ attribution:'&copy; OpenStreetMap'}).addTo(map)
    let marker:any=null; map.on('click',(e:any)=>{ if(marker) marker.remove(); marker=L.marker(e.latlng).addTo(map); setLat(e.latlng.lat); setLng(e.latlng.lng) })
    return ()=>map.remove()
  },[])

  async function submit(){
    if(!assetTypeId||!name.trim()){ alert('Choose type & name'); return }
    const res = await axios.post('/api/assets', { assetTypeId, name, region, city, latitude:lat, longitude:lng, fieldValues:values })
    const asset = res.data
    for(const f of fields.filter(x=>x.dataType==='Attachment')){
      const file = attachments[f.name]; if(file){ const form=new FormData(); form.append('file', file); await axios.post(`/api/assets/${asset.id}/documents`, form, { headers: {'Content-Type':'multipart/form-data'} }) }
    }
    alert('Created asset #' + asset.id)
  }
  return (<div>
    <h3>Create Asset</h3>
    <div style={{display:'grid',gridTemplateColumns:'1fr 1fr',gap:12}}>
      <div>
        <label>Type <select value={assetTypeId} onChange={e=>setAssetTypeId(parseInt(e.target.value))}>
          <option value="">--</option>{types.map(t=><option key={t.id} value={t.id}>{t.name}</option>)}
        </select></label><br/>
        <label>Name<br/><input value={name} onChange={e=>setName(e.target.value)}/></label><br/>
        <label>Region<br/><input value={region} onChange={e=>setRegion(e.target.value)}/></label><br/>
        <label>City<br/><input value={city} onChange={e=>setCity(e.target.value)}/></label><br/>
        <div id="map" style={{height:300,border:'1px solid #ccc',margin:'8px 0'}}></div>
        <div>Lat/Lng: {lat?.toFixed(6)}, {lng?.toFixed(6)}</div>
      </div>
      <div>
        <h4>Dynamic Fields</h4>
        {fields.map(f => (<div key={f.id} style={{marginBottom:8}}>
          {f.dataType!=='Attachment' && f.dataType!=='Location' && (<>
            <label>{f.name}{f.required?' *':''}</label><br/>
            {f.dataType==='Text' && <input onChange={e=>setValues(prev=>({...prev,[f.name]:e.target.value}))}/>}
            {f.dataType==='Number' && <input type="number" onChange={e=>setValues(prev=>({...prev,[f.name]:e.target.value}))}/>}
            {f.dataType==='Date' && <input type="date" onChange={e=>setValues(prev=>({...prev,[f.name]:e.target.value}))}/>}
            {f.dataType==='Dropdown' && <select onChange={e=>setValues(prev=>({...prev,[f.name]:e.target.value}))}><option value=""></option>{(f.optionsCsv||'').split(',').filter(x=>x).map(o=><option key={o.trim()}>{o.trim()}</option>)}</select>}
          </>)}
          {f.dataType==='Attachment' && (<><label>{f.name} (Attachment)</label><br/><input type="file" onChange={e=>setAttachments(prev=>({...prev,[f.name]:e.target.files?.[0]||null}))}/></>)}
          {f.dataType==='Location' && <div>(Use map)</div>}
        </div>))}
        <button onClick={submit}>Create</button>
      </div>
    </div>
  </div>)
}
