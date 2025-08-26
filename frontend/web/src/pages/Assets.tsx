import { useEffect,useState } from 'react'; import axios from 'axios'
type Item={id:number,name:string,region:string,city:string,status:string,assetTypeId:number}
export default function Assets(){
  const [items,setItems]=useState<Item[]>([]); const [total,setTotal]=useState(0)
  async function load(){ const r=await axios.get('/api/assets?page=1&pageSize=50'); setItems(r.data.items); setTotal(r.data.total); }
  useEffect(()=>{load()},[])
  return (<div><h3>Assets ({total})</h3><table border={1} cellPadding={6}>
    <thead><tr><th>ID</th><th>Name</th><th>Region</th><th>City</th><th>Status</th><th>Type</th></tr></thead>
    <tbody>{items.map(a=>(<tr key={a.id}><td>{a.id}</td><td>{a.name}</td><td>{a.region}</td><td>{a.city}</td><td>{a.status}</td><td>{a.assetTypeId}</td></tr>))}</tbody>
  </table></div>)
}
