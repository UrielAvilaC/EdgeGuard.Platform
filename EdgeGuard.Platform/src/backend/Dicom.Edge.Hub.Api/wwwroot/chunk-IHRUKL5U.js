import{a as w,b as _}from"./chunk-WOG5XNLC.js";import{da as D}from"./chunk-US65MP6N.js";import{Ac as p,Cb as c,Db as o,Eb as m,Fb as s,Sb as l,Tb as x,Ub as C,Wa as d,Y as g,_ as u,aa as f,cc as M,jb as r,kb as v,lb as y,xb as h,yb as b}from"./chunk-4AVS2LI2.js";function S(t,i){if(t&1&&(o(0,"div",0),s(1,"mat-spinner",2),m()),t&2){let e=l();d(),c("diameter",e.diameter())}}function k(t,i){if(t&1&&(o(0,"div",1),s(1,"mat-spinner",2),m()),t&2){let e=l();d(),c("diameter",e.diameter())}}var A=class t{overlay=p(!1);diameter=p(40);static \u0275fac=function(e){return new(e||t)};static \u0275cmp=r({type:t,selectors:[["ui-loading-spinner"]],inputs:{overlay:[1,"overlay"],diameter:[1,"diameter"]},decls:2,vars:1,consts:[[1,"spinner-overlay"],[1,"spinner-inline"],[3,"diameter"]],template:function(e,a){e&1&&h(0,S,2,1,"div",0)(1,k,2,1,"div",1),e&2&&b(a.overlay()?0:1)},dependencies:[_,w],styles:["[_nghost-%COMP%]{display:block}.spinner-overlay[_ngcontent-%COMP%]{position:absolute;inset:0;z-index:10;display:flex;align-items:center;justify-content:center;background:#fffc;-webkit-backdrop-filter:blur(4px);backdrop-filter:blur(4px);border-radius:inherit;transition:opacity var(--eg-transition-normal, .25s)}.dark[_nghost-%COMP%]   .spinner-overlay[_ngcontent-%COMP%], .dark   [_nghost-%COMP%]   .spinner-overlay[_ngcontent-%COMP%]{background:#0f172acc}.spinner-inline[_ngcontent-%COMP%]{display:flex;align-items:center;justify-content:center;padding:24px}"],changeDetection:0})};var j=["*"];var F=new u("MAT_CARD_CONFIG"),Q=(()=>{class t{appearance;constructor(){let e=f(F,{optional:!0});this.appearance=e?.appearance||"raised"}static \u0275fac=function(a){return new(a||t)};static \u0275cmp=r({type:t,selectors:[["mat-card"]],hostAttrs:[1,"mat-mdc-card","mdc-card"],hostVars:8,hostBindings:function(a,n){a&2&&M("mat-mdc-card-outlined",n.appearance==="outlined")("mdc-card--outlined",n.appearance==="outlined")("mat-mdc-card-filled",n.appearance==="filled")("mdc-card--filled",n.appearance==="filled")},inputs:{appearance:"appearance"},exportAs:["matCard"],ngContentSelectors:j,decls:1,vars:0,template:function(a,n){a&1&&(x(),C(0))},styles:[`.mat-mdc-card {
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  position: relative;
  border-style: solid;
  border-width: 0;
  background-color: var(--mat-card-elevated-container-color, var(--mat-sys-surface-container-low));
  border-color: var(--mat-card-elevated-container-color, var(--mat-sys-surface-container-low));
  border-radius: var(--mat-card-elevated-container-shape, var(--mat-sys-corner-medium));
  box-shadow: var(--mat-card-elevated-container-elevation, var(--mat-sys-level1));
}
.mat-mdc-card::after {
  position: absolute;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  border: solid 1px transparent;
  content: "";
  display: block;
  pointer-events: none;
  box-sizing: border-box;
  border-radius: var(--mat-card-elevated-container-shape, var(--mat-sys-corner-medium));
}

.mat-mdc-card-outlined {
  background-color: var(--mat-card-outlined-container-color, var(--mat-sys-surface));
  border-radius: var(--mat-card-outlined-container-shape, var(--mat-sys-corner-medium));
  border-width: var(--mat-card-outlined-outline-width, 1px);
  border-color: var(--mat-card-outlined-outline-color, var(--mat-sys-outline-variant));
  box-shadow: var(--mat-card-outlined-container-elevation, var(--mat-sys-level0));
}
.mat-mdc-card-outlined::after {
  border: none;
}

.mat-mdc-card-filled {
  background-color: var(--mat-card-filled-container-color, var(--mat-sys-surface-container-highest));
  border-radius: var(--mat-card-filled-container-shape, var(--mat-sys-corner-medium));
  box-shadow: var(--mat-card-filled-container-elevation, var(--mat-sys-level0));
}

.mdc-card__media {
  position: relative;
  box-sizing: border-box;
  background-repeat: no-repeat;
  background-position: center;
  background-size: cover;
}
.mdc-card__media::before {
  display: block;
  content: "";
}
.mdc-card__media:first-child {
  border-top-left-radius: inherit;
  border-top-right-radius: inherit;
}
.mdc-card__media:last-child {
  border-bottom-left-radius: inherit;
  border-bottom-right-radius: inherit;
}

.mat-mdc-card-actions {
  display: flex;
  flex-direction: row;
  align-items: center;
  box-sizing: border-box;
  min-height: 52px;
  padding: 8px;
}

.mat-mdc-card-title {
  font-family: var(--mat-card-title-text-font, var(--mat-sys-title-large-font));
  line-height: var(--mat-card-title-text-line-height, var(--mat-sys-title-large-line-height));
  font-size: var(--mat-card-title-text-size, var(--mat-sys-title-large-size));
  letter-spacing: var(--mat-card-title-text-tracking, var(--mat-sys-title-large-tracking));
  font-weight: var(--mat-card-title-text-weight, var(--mat-sys-title-large-weight));
}

.mat-mdc-card-subtitle {
  color: var(--mat-card-subtitle-text-color, var(--mat-sys-on-surface));
  font-family: var(--mat-card-subtitle-text-font, var(--mat-sys-title-medium-font));
  line-height: var(--mat-card-subtitle-text-line-height, var(--mat-sys-title-medium-line-height));
  font-size: var(--mat-card-subtitle-text-size, var(--mat-sys-title-medium-size));
  letter-spacing: var(--mat-card-subtitle-text-tracking, var(--mat-sys-title-medium-tracking));
  font-weight: var(--mat-card-subtitle-text-weight, var(--mat-sys-title-medium-weight));
}

.mat-mdc-card-title,
.mat-mdc-card-subtitle {
  display: block;
  margin: 0;
}
.mat-mdc-card-avatar ~ .mat-mdc-card-header-text .mat-mdc-card-title,
.mat-mdc-card-avatar ~ .mat-mdc-card-header-text .mat-mdc-card-subtitle {
  padding: 16px 16px 0;
}

.mat-mdc-card-header {
  display: flex;
  padding: 16px 16px 0;
}

.mat-mdc-card-content {
  display: block;
  padding: 0 16px;
}
.mat-mdc-card-content:first-child {
  padding-top: 16px;
}
.mat-mdc-card-content:last-child {
  padding-bottom: 16px;
}

.mat-mdc-card-title-group {
  display: flex;
  justify-content: space-between;
  width: 100%;
}

.mat-mdc-card-avatar {
  height: 40px;
  width: 40px;
  border-radius: 50%;
  flex-shrink: 0;
  margin-bottom: 16px;
  object-fit: cover;
}
.mat-mdc-card-avatar ~ .mat-mdc-card-header-text .mat-mdc-card-subtitle,
.mat-mdc-card-avatar ~ .mat-mdc-card-header-text .mat-mdc-card-title {
  line-height: normal;
}

.mat-mdc-card-sm-image {
  width: 80px;
  height: 80px;
}

.mat-mdc-card-md-image {
  width: 112px;
  height: 112px;
}

.mat-mdc-card-lg-image {
  width: 152px;
  height: 152px;
}

.mat-mdc-card-xl-image {
  width: 240px;
  height: 240px;
}

.mat-mdc-card-subtitle ~ .mat-mdc-card-title,
.mat-mdc-card-title ~ .mat-mdc-card-subtitle,
.mat-mdc-card-header .mat-mdc-card-header-text .mat-mdc-card-title,
.mat-mdc-card-header .mat-mdc-card-header-text .mat-mdc-card-subtitle,
.mat-mdc-card-title-group .mat-mdc-card-title,
.mat-mdc-card-title-group .mat-mdc-card-subtitle {
  padding-top: 0;
}

.mat-mdc-card-content > :last-child:not(.mat-mdc-card-footer) {
  margin-bottom: 0;
}

.mat-mdc-card-actions-align-end {
  justify-content: flex-end;
}
`],encapsulation:2,changeDetection:0})}return t})();var W=(()=>{class t{static \u0275fac=function(a){return new(a||t)};static \u0275dir=y({type:t,selectors:[["mat-card-content"]],hostAttrs:[1,"mat-mdc-card-content"]})}return t})();var Y=(()=>{class t{static \u0275fac=function(a){return new(a||t)};static \u0275mod=v({type:t});static \u0275inj=g({imports:[D]})}return t})();export{A as a,Q as b,W as c,Y as d};
