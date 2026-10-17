(() => {
'use strict';
if (typeof Chart === 'undefined') return;
Chart.defaults.color = '#d2dfd4'; Chart.defaults.borderColor = '#bda15d25';
const source = document.getElementById('appointment-trend-data'), canvas = document.getElementById('appointment-trend');
if (source && canvas) {
 const points = JSON.parse(source.textContent);
 new Chart(canvas, { type:'line', data:{ labels:points.map(x=>x.Date), datasets:[{label:'Randevu',data:points.map(x=>x.Count),borderColor:'#70c49a',backgroundColor:'#70c49a22',fill:true,tension:0.35,pointRadius:3}] }, options:{responsive:true,maintainAspectRatio:false,scales:{y:{beginAtZero:true,ticks:{precision:0}}},plugins:{legend:{display:false}}} });
}
const distribution = document.getElementById('service-distribution-data'), ring = document.getElementById('service-distribution');
if (distribution && ring) {
 const points = JSON.parse(distribution.textContent);
 new Chart(ring,{type:'doughnut',data:{labels:points.map(x=>x.Service),datasets:[{data:points.map(x=>x.Count),backgroundColor:['#d2ae60','#70ad8a','#c6d7b0','#a98770','#719c9a','#d6c69c','#9c92ad','#ceaa99'],borderWidth:0}]},options:{responsive:true,maintainAspectRatio:false,cutout:'68%',plugins:{legend:{position:'bottom',labels:{boxWidth:12}}}}});
}
})();
