const segs = [];
const openings = [];

const segLen = () => parseFloat(document.getElementById('segLen').value || '0');
const segAng = () => parseFloat(document.getElementById('segAng').value || '0');

const canvas = document.getElementById('sketch');
const ctx = canvas.getContext('2d');

function polyPoints(origin, segments){
  let x = origin[0], y = origin[1];
  const pts = [[x,y]];
  for(const s of segments){
    const rad = (Number(s.angle_deg) * Math.PI) / 180.0;
    x += Number(s.length_ft) * Math.cos(rad);
    y += Number(s.length_ft) * Math.sin(rad);
    pts.push([x,y]);
  }
  return pts;
}

function perimeter(pts){
  let p=0;
  for(let i=0;i<pts.length-1;i++){
    const [x1,y1]=pts[i], [x2,y2]=pts[i+1];
    p += Math.hypot(x2-x1, y2-y1);
  }
  // close back to origin
  if(pts.length>1){
    const [x1,y1]=pts[pts.length-1], [x2,y2]=pts[0];
    p += Math.hypot(x2-x1, y2-y1);
  }
  return p;
}

function area(pts){
  let s=0;
  for(let i=0;i<pts.length;i++){
    const [x1,y1]=pts[i], [x2,y2]=pts[(i+1)%pts.length];
    s += x1*y2 - x2*y1;
  }
  return Math.abs(s)/2.0;
}

function redraw(){
  const pts = polyPoints([0,0], segs);
  // autoscale
  const xs = pts.map(p=>p[0]), ys=pts.map(p=>p[1]);
  const minx=Math.min(...xs,0), maxx=Math.max(...xs,0);
  const miny=Math.min(...ys,0), maxy=Math.max(...ys,0);
  const w=maxx-minx || 1, h=maxy-miny || 1;
  const pad=20;
  const sx=(canvas.width-2*pad)/w, sy=(canvas.height-2*pad)/h;
  const s=Math.min(sx, sy);
  const tx = (v)=> pad + (v-minx)*s;
  const ty = (v)=> canvas.height - (pad + (v-miny)*s);

  ctx.clearRect(0,0,canvas.width,canvas.height);

  // draw polyline
  ctx.beginPath();
  pts.forEach(([x,y],i)=>{
    const X=tx(x), Y=ty(y);
    if(i===0) ctx.moveTo(X,Y); else ctx.lineTo(X,Y);
  });
  // close
  if(pts.length>1){
    const [x0,y0]=pts[0];
    ctx.lineTo(tx(x0), ty(y0));
  }
  ctx.stroke();

  // draw vertices
  pts.forEach(([x,y])=>{
    const X=tx(x), Y=ty(y);
    ctx.beginPath(); ctx.arc(X,Y,3,0,Math.PI*2); ctx.fill();
  });

  document.getElementById('area').innerText = area(pts).toFixed(2);
  document.getElementById('perimeter').innerText = perimeter(pts).toFixed(2);

  // store for submit
  document.querySelector('input[name="segments_json"]').value = JSON.stringify(segs);
  document.querySelector('input[name="openings_json"]').value = JSON.stringify(openings);
}

document.getElementById('addSeg').addEventListener('click', (e)=>{
  e.preventDefault();
  const L=segLen(), A=segAng();
  if(L>0 || A===0){ segs.push({length_ft:L, angle_deg:A}); redraw(); }
});

document.getElementById('undoSeg').addEventListener('click', (e)=>{
  e.preventDefault(); segs.pop(); redraw();
});

document.getElementById('closePoly').addEventListener('click', (e)=>{
  e.preventDefault(); redraw(); // visual close is automatic in redraw
});

window.addEventListener('load', redraw);
