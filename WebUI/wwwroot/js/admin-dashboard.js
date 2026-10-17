(() => {
'use strict';
if (typeof Chart === 'undefined') return;
Chart.defaults.color = '#d2dfd4'; Chart.defaults.borderColor = '#bda15d25';
const source = document.getElementById('appointment-trend-data'), canvas = document.getElementById('appointment-trend');
if (source && canvas) {
 const points = JSON.parse(source.textContent);
 new Chart(canvas, { type:'line', data:{ labels:points.map(x=>x.Date), datasets:[{label:'Randevu',data:points.map(x=>x.Count),borderColor:'#70c49a',backgroundColor:'#70c49a22',fill:true,tension:0.35,pointRadius:3}] }, options:{responsive:true,maintainAspectRatio:false,scales:{y:{beginAtZero:true,ticks:{precision:0}}},plugins:{legend:{display:false}}} });
}
})();
