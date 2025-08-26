import React from 'react'
import ReactDOM from 'react-dom/client'
import { createBrowserRouter, RouterProvider, Link } from 'react-router-dom'
import Home from './pages/Home'
import Admin from './pages/Admin'
import Assets from './pages/Assets'
import AssetCreate from './pages/AssetCreate'

const router = createBrowserRouter([
  { path: '/', element: <Home/> },
  { path: '/admin', element: <Admin/> },
  { path: '/assets', element: <Assets/> },
  { path: '/assets/create', element: <AssetCreate/> },
])

function Shell(){return (<div><header style={{display:'flex',gap:16,alignItems:'center',marginBottom:16}}>
  <h2 style={{margin:0}}>Dynamic Asset Platform (PRO)</h2>
  <nav style={{display:'flex',gap:12}}>
    <Link to="/">Home</Link><Link to="/admin">Admin</Link><Link to="/assets">Assets</Link><Link to="/assets/create">Create</Link>
  </nav></header><RouterProvider router={router} /></div>)}

ReactDOM.createRoot(document.getElementById('root')!).render(<React.StrictMode><Shell/></React.StrictMode>)
