import{b as Lt,c as Ut}from"./chunk-5KXKONXZ.js";import{a as Vt}from"./chunk-NXOC3EOT.js";import{a as Et}from"./chunk-UH4GXGNA.js";import{b as Ot,c as At}from"./chunk-OMXJFTYP.js";import{a as Bt,c as Ht}from"./chunk-J6WEWCJW.js";import{a as Fe,b as It,c as Ft,f as Nt,g as zt,i as Pt}from"./chunk-KQGDE6EM.js";import{E as Dt,G as Ee,R as Rt,U as St,ba as Tt,ca as Ie,d as Ct,da as Mt,ea as ee,r as wt,w as xt,x as Me}from"./chunk-KXL2KJSG.js";import{$a as de,A as ht,Bb as bt,Cb as gt,Cc as Ue,Db as x,Eb as u,Ec as Te,Fb as f,Fc as be,Gb as q,Gc as V,Hb as K,Ib as $,Jb as ie,Kb as me,Kc as J,Lb as he,Lc as se,Mb as D,Nb as ue,Nc as p,Ob as kt,Oc as ge,Q as ne,Rb as A,Tb as b,Ub as Z,Vb as I,W as ut,Wa as d,Wb as fe,Xb as Le,Y as L,Yb as T,Zb as M,_ as j,aa as s,ab as B,bc as yt,cb as _t,cc as Y,db as Re,dc as N,e as Q,ec as Se,f as at,fa as ce,fb as oe,fc as pe,g as st,ga as le,h as ct,ha as we,hc as _e,i as lt,ia as ft,ja as pt,jb as v,ka as He,kb as U,l as Be,lb as m,n as dt,na as X,nb as y,oa as xe,ob as ae,q as Ce,qc as F,sa as De,sc as vt,wa as W,wb as G,xa as P,xb as C,y as mt,yb as w,za as S}from"./chunk-T2F57IQD.js";var _n=[[["caption"]],[["colgroup"],["col"]],"*"],bn=["caption","colgroup, col","*"];function gn(n,o){n&1&&I(0,2)}function kn(n,o){n&1&&(u(0,"thead",0),D(1,1),f(),u(2,"tbody",0),D(3,2)(4,3),f(),u(5,"tfoot",0),D(6,4),f())}function yn(n,o){n&1&&D(0,1)(1,2)(2,3)(3,4)}var H=new j("CDK_TABLE");var Ne=(()=>{class n{template=s(B);constructor(){}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkCellDef",""]]})}return n})(),ze=(()=>{class n{template=s(B);constructor(){}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkHeaderCellDef",""]]})}return n})(),qt=(()=>{class n{template=s(B);constructor(){}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkFooterCellDef",""]]})}return n})(),te=(()=>{class n{_table=s(H,{optional:!0});_hasStickyChanged=!1;get name(){return this._name}set name(e){this._setNameInput(e)}_name;get sticky(){return this._sticky}set sticky(e){e!==this._sticky&&(this._sticky=e,this._hasStickyChanged=!0)}_sticky=!1;get stickyEnd(){return this._stickyEnd}set stickyEnd(e){e!==this._stickyEnd&&(this._stickyEnd=e,this._hasStickyChanged=!0)}_stickyEnd=!1;cell;headerCell;footerCell;cssClassFriendlyName;_columnCssClassName;constructor(){}hasStickyChanged(){let e=this._hasStickyChanged;return this.resetStickyChanged(),e}resetStickyChanged(){this._hasStickyChanged=!1}_updateColumnCssClassName(){this._columnCssClassName=[`cdk-column-${this.cssClassFriendlyName}`]}_setNameInput(e){e&&(this._name=e,this.cssClassFriendlyName=e.replace(/[^a-z0-9_-]/gi,"-"),this._updateColumnCssClassName())}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkColumnDef",""]],contentQueries:function(t,i,r){if(t&1&&fe(r,Ne,5)(r,ze,5)(r,qt,5),t&2){let a;T(a=M())&&(i.cell=a.first),T(a=M())&&(i.headerCell=a.first),T(a=M())&&(i.footerCell=a.first)}},inputs:{name:[0,"cdkColumnDef","name"],sticky:[2,"sticky","sticky",p],stickyEnd:[2,"stickyEnd","stickyEnd",p]}})}return n})(),Ae=class{constructor(o,e){e.nativeElement.classList.add(...o._columnCssClassName)}},$t=(()=>{class n extends Ae{constructor(){super(s(te),s(S))}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["cdk-header-cell"],["th","cdk-header-cell",""]],hostAttrs:["role","columnheader",1,"cdk-header-cell"],features:[y]})}return n})();var Qt=(()=>{class n extends Ae{constructor(){let e=s(te),t=s(S);super(e,t);let i=e._table?._getCellRole();i&&t.nativeElement.setAttribute("role",i)}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["cdk-cell"],["td","cdk-cell",""]],hostAttrs:[1,"cdk-cell"],features:[y]})}return n})();var je=(()=>{class n{template=s(B);_differs=s(se);columns;_columnsDiffer;constructor(){}ngOnChanges(e){if(!this._columnsDiffer){let t=e.columns&&e.columns.currentValue||[];this._columnsDiffer=this._differs.find(t).create(),this._columnsDiffer.diff(t)}}getColumnsDiff(){return this._columnsDiffer.diff(this.columns)}extractCellTemplate(e){return this instanceof ve?e.headerCell.template:this instanceof Xe?e.footerCell.template:e.cell.template}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,features:[W]})}return n})(),ve=(()=>{class n extends je{_table=s(H,{optional:!0});_hasStickyChanged=!1;get sticky(){return this._sticky}set sticky(e){e!==this._sticky&&(this._sticky=e,this._hasStickyChanged=!0)}_sticky=!1;constructor(){super(s(B),s(se))}ngOnChanges(e){super.ngOnChanges(e)}hasStickyChanged(){let e=this._hasStickyChanged;return this.resetStickyChanged(),e}resetStickyChanged(){this._hasStickyChanged=!1}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkHeaderRowDef",""]],inputs:{columns:[0,"cdkHeaderRowDef","columns"],sticky:[2,"cdkHeaderRowDefSticky","sticky",p]},features:[y,W]})}return n})(),Xe=(()=>{class n extends je{_table=s(H,{optional:!0});_hasStickyChanged=!1;get sticky(){return this._sticky}set sticky(e){e!==this._sticky&&(this._sticky=e,this._hasStickyChanged=!0)}_sticky=!1;constructor(){super(s(B),s(se))}ngOnChanges(e){super.ngOnChanges(e)}hasStickyChanged(){let e=this._hasStickyChanged;return this.resetStickyChanged(),e}resetStickyChanged(){this._hasStickyChanged=!1}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkFooterRowDef",""]],inputs:{columns:[0,"cdkFooterRowDef","columns"],sticky:[2,"cdkFooterRowDefSticky","sticky",p]},features:[y,W]})}return n})(),Pe=(()=>{class n extends je{_table=s(H,{optional:!0});when;constructor(){super(s(B),s(se))}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkRowDef",""]],inputs:{columns:[0,"cdkRowDefColumns","columns"],when:[0,"cdkRowDefWhen","when"]},features:[y]})}return n})(),re=(()=>{class n{_viewContainer=s(oe);cells;context;static mostRecentCellOutlet=null;constructor(){n.mostRecentCellOutlet=this}ngOnDestroy(){n.mostRecentCellOutlet===this&&(n.mostRecentCellOutlet=null)}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","cdkCellOutlet",""]]})}return n})(),qe=(()=>{class n{static \u0275fac=function(t){return new(t||n)};static \u0275cmp=v({type:n,selectors:[["cdk-header-row"],["tr","cdk-header-row",""]],hostAttrs:["role","row",1,"cdk-header-row"],decls:1,vars:0,consts:[["cdkCellOutlet",""]],template:function(t,i){t&1&&D(0,0)},dependencies:[re],encapsulation:2})}return n})();var $e=(()=>{class n{static \u0275fac=function(t){return new(t||n)};static \u0275cmp=v({type:n,selectors:[["cdk-row"],["tr","cdk-row",""]],hostAttrs:["role","row",1,"cdk-row"],decls:1,vars:0,consts:[["cdkCellOutlet",""]],template:function(t,i){t&1&&D(0,0)},dependencies:[re],encapsulation:2})}return n})(),Wt=(()=>{class n{templateRef=s(B);_contentClassNames=["cdk-no-data-row","cdk-row"];_cellClassNames=["cdk-cell","cdk-no-data-cell"];_cellSelector="td, cdk-cell, [cdk-cell], .cdk-cell";constructor(){}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["ng-template","cdkNoDataRow",""]]})}return n})(),jt=["top","bottom","left","right"],Ve=class{_isNativeHtmlTable;_stickCellCss;_isBrowser;_needsPositionStickyOnElement;direction;_positionListener;_tableInjector;_elemSizeCache=new WeakMap;_resizeObserver=globalThis?.ResizeObserver?new globalThis.ResizeObserver(o=>this._updateCachedSizes(o)):null;_updatedStickyColumnsParamsToReplay=[];_stickyColumnsReplayTimeout=null;_cachedCellWidths=[];_borderCellCss;_destroyed=!1;constructor(o,e,t=!0,i=!0,r,a,c){this._isNativeHtmlTable=o,this._stickCellCss=e,this._isBrowser=t,this._needsPositionStickyOnElement=i,this.direction=r,this._positionListener=a,this._tableInjector=c,this._borderCellCss={top:`${e}-border-elem-top`,bottom:`${e}-border-elem-bottom`,left:`${e}-border-elem-left`,right:`${e}-border-elem-right`}}clearStickyPositioning(o,e){(e.includes("left")||e.includes("right"))&&this._removeFromStickyColumnReplayQueue(o);let t=[];for(let i of o)i.nodeType===i.ELEMENT_NODE&&t.push(i,...Array.from(i.children));de({write:()=>{for(let i of t)this._removeStickyStyle(i,e)}},{injector:this._tableInjector})}updateStickyColumns(o,e,t,i=!0,r=!0){if(!o.length||!this._isBrowser||!(e.some(z=>z)||t.some(z=>z))){this._positionListener?.stickyColumnsUpdated({sizes:[]}),this._positionListener?.stickyEndColumnsUpdated({sizes:[]});return}let a=o[0],c=a.children.length,l=this.direction==="rtl",h=l?"right":"left",_=l?"left":"right",O=e.lastIndexOf(!0),g=t.indexOf(!0),k,it,rt;r&&this._updateStickyColumnReplayQueue({rows:[...o],stickyStartStates:[...e],stickyEndStates:[...t]}),de({earlyRead:()=>{k=this._getCellWidths(a,i),it=this._getStickyStartColumnPositions(k,e),rt=this._getStickyEndColumnPositions(k,t)},write:()=>{for(let z of o)for(let E=0;E<c;E++){let ot=z.children[E];e[E]&&this._addStickyStyle(ot,h,it[E],E===O),t[E]&&this._addStickyStyle(ot,_,rt[E],E===g)}this._positionListener&&k.some(z=>!!z)&&(this._positionListener.stickyColumnsUpdated({sizes:O===-1?[]:k.slice(0,O+1).map((z,E)=>e[E]?z:null)}),this._positionListener.stickyEndColumnsUpdated({sizes:g===-1?[]:k.slice(g).map((z,E)=>t[E+g]?z:null).reverse()}))}},{injector:this._tableInjector})}stickRows(o,e,t){if(!this._isBrowser)return;let i=t==="bottom"?o.slice().reverse():o,r=t==="bottom"?e.slice().reverse():e,a=[],c=[],l=[];de({earlyRead:()=>{for(let h=0,_=0;h<i.length;h++){if(!r[h])continue;a[h]=_;let O=i[h];l[h]=this._isNativeHtmlTable?Array.from(O.children):[O];let g=this._retrieveElementSize(O).height;_+=g,c[h]=g}},write:()=>{let h=r.lastIndexOf(!0);for(let _=0;_<i.length;_++){if(!r[_])continue;let O=a[_],g=_===h;for(let k of l[_])this._addStickyStyle(k,t,O,g)}t==="top"?this._positionListener?.stickyHeaderRowsUpdated({sizes:c,offsets:a,elements:l}):this._positionListener?.stickyFooterRowsUpdated({sizes:c,offsets:a,elements:l})}},{injector:this._tableInjector})}updateStickyFooterContainer(o,e){this._isNativeHtmlTable&&de({write:()=>{let t=o.querySelector("tfoot");t&&(e.some(i=>!i)?this._removeStickyStyle(t,["bottom"]):this._addStickyStyle(t,"bottom",0,!1))}},{injector:this._tableInjector})}destroy(){this._stickyColumnsReplayTimeout&&clearTimeout(this._stickyColumnsReplayTimeout),this._resizeObserver?.disconnect(),this._destroyed=!0}_removeStickyStyle(o,e){if(!o.classList.contains(this._stickCellCss))return;for(let i of e)o.style[i]="",o.classList.remove(this._borderCellCss[i]);jt.some(i=>e.indexOf(i)===-1&&o.style[i])?o.style.zIndex=this._getCalculatedZIndex(o):(o.style.zIndex="",this._needsPositionStickyOnElement&&(o.style.position=""),o.classList.remove(this._stickCellCss))}_addStickyStyle(o,e,t,i){o.classList.add(this._stickCellCss),i&&o.classList.add(this._borderCellCss[e]),o.style[e]=`${t}px`,o.style.zIndex=this._getCalculatedZIndex(o),this._needsPositionStickyOnElement&&(o.style.cssText+="position: -webkit-sticky; position: sticky; ")}_getCalculatedZIndex(o){let e={top:100,bottom:10,left:1,right:1},t=0;for(let i of jt)o.style[i]&&(t+=e[i]);return t?`${t}`:""}_getCellWidths(o,e=!0){if(!e&&this._cachedCellWidths.length)return this._cachedCellWidths;let t=[],i=o.children;for(let r=0;r<i.length;r++){let a=i[r];t.push(this._retrieveElementSize(a).width)}return this._cachedCellWidths=t,t}_getStickyStartColumnPositions(o,e){let t=[],i=0;for(let r=0;r<o.length;r++)e[r]&&(t[r]=i,i+=o[r]);return t}_getStickyEndColumnPositions(o,e){let t=[],i=0;for(let r=o.length;r>0;r--)e[r]&&(t[r]=i,i+=o[r]);return t}_retrieveElementSize(o){let e=this._elemSizeCache.get(o);if(e)return e;let t=o.getBoundingClientRect(),i={width:t.width,height:t.height};return this._resizeObserver&&(this._elemSizeCache.set(o,i),this._resizeObserver.observe(o,{box:"border-box"})),i}_updateStickyColumnReplayQueue(o){this._removeFromStickyColumnReplayQueue(o.rows),this._stickyColumnsReplayTimeout||this._updatedStickyColumnsParamsToReplay.push(o)}_removeFromStickyColumnReplayQueue(o){let e=new Set(o);for(let t of this._updatedStickyColumnsParamsToReplay)t.rows=t.rows.filter(i=>!e.has(i));this._updatedStickyColumnsParamsToReplay=this._updatedStickyColumnsParamsToReplay.filter(t=>!!t.rows.length)}_updateCachedSizes(o){let e=!1;for(let t of o){let i=t.borderBoxSize?.length?{width:t.borderBoxSize[0].inlineSize,height:t.borderBoxSize[0].blockSize}:{width:t.contentRect.width,height:t.contentRect.height};i.width!==this._elemSizeCache.get(t.target)?.width&&vn(t.target)&&(e=!0),this._elemSizeCache.set(t.target,i)}e&&this._updatedStickyColumnsParamsToReplay.length&&(this._stickyColumnsReplayTimeout&&clearTimeout(this._stickyColumnsReplayTimeout),this._stickyColumnsReplayTimeout=setTimeout(()=>{if(!this._destroyed){for(let t of this._updatedStickyColumnsParamsToReplay)this.updateStickyColumns(t.rows,t.stickyStartStates,t.stickyEndStates,!0,!1);this._updatedStickyColumnsParamsToReplay=[],this._stickyColumnsReplayTimeout=null}},0))}};function vn(n){return["cdk-cell","cdk-header-cell","cdk-footer-cell"].some(o=>n.classList.contains(o))}var ke=new j("STICKY_POSITIONING_LISTENER");var Qe=(()=>{class n{viewContainer=s(oe);elementRef=s(S);constructor(){let e=s(H);e._rowOutlet=this,e._outletAssigned()}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","rowOutlet",""]]})}return n})(),We=(()=>{class n{viewContainer=s(oe);elementRef=s(S);constructor(){let e=s(H);e._headerRowOutlet=this,e._outletAssigned()}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","headerRowOutlet",""]]})}return n})(),Ge=(()=>{class n{viewContainer=s(oe);elementRef=s(S);constructor(){let e=s(H);e._footerRowOutlet=this,e._outletAssigned()}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","footerRowOutlet",""]]})}return n})(),Ke=(()=>{class n{viewContainer=s(oe);elementRef=s(S);constructor(){let e=s(H);e._noDataRowOutlet=this,e._outletAssigned()}static \u0275fac=function(t){return new(t||n)};static \u0275dir=m({type:n,selectors:[["","noDataRowOutlet",""]]})}return n})(),Ze=(()=>{class n{_differs=s(se);_changeDetectorRef=s(J);_elementRef=s(S);_dir=s(Mt,{optional:!0});_platform=s(wt);_viewRepeater;_viewportRuler=s(Nt);_injector=s(pt);_virtualScrollViewport=s(zt,{optional:!0,host:!0});_positionListener=s(ke,{optional:!0})||s(ke,{optional:!0,skipSelf:!0});_document=s(He);_data;_renderedRange;_onDestroy=new Q;_renderRows;_renderChangeSubscription=null;_columnDefsByName=new Map;_rowDefs;_headerRowDefs;_footerRowDefs;_dataDiffer;_defaultRowDef=null;_customColumnDefs=new Set;_customRowDefs=new Set;_customHeaderRowDefs=new Set;_customFooterRowDefs=new Set;_customNoDataRow=null;_headerRowDefChanged=!0;_footerRowDefChanged=!0;_stickyColumnStylesNeedReset=!0;_forceRecalculateCellWidths=!0;_cachedRenderRowsMap=new Map;_isNativeHtmlTable;_stickyStyler;stickyCssClass="cdk-table-sticky";needsPositionStickyOnElement=!0;_isServer;_isShowingNoDataRow=!1;_hasAllOutlets=!1;_hasInitialized=!1;_headerRowStickyUpdates=new Q;_footerRowStickyUpdates=new Q;_disableVirtualScrolling=!1;_getCellRole(){if(this._cellRoleInternal===void 0){let e=this._elementRef.nativeElement.getAttribute("role");return e==="grid"||e==="treegrid"?"gridcell":"cell"}return this._cellRoleInternal}_cellRoleInternal=void 0;get trackBy(){return this._trackByFn}set trackBy(e){this._trackByFn=e}_trackByFn;get dataSource(){return this._dataSource}set dataSource(e){this._dataSource!==e&&(this._switchDataSource(e),this._changeDetectorRef.markForCheck())}_dataSource;_dataSourceChanges=new Q;_dataStream=new Q;get multiTemplateDataRows(){return this._multiTemplateDataRows}set multiTemplateDataRows(e){this._multiTemplateDataRows=e,this._rowOutlet&&this._rowOutlet.viewContainer.length&&(this._forceRenderDataRows(),this.updateStickyColumnStyles())}_multiTemplateDataRows=!1;get fixedLayout(){return this._virtualScrollEnabled()?!0:this._fixedLayout}set fixedLayout(e){this._fixedLayout=e,this._forceRecalculateCellWidths=!0,this._stickyColumnStylesNeedReset=!0}_fixedLayout=!1;recycleRows=!1;contentChanged=new X;viewChange=new at({start:0,end:Number.MAX_VALUE});_rowOutlet;_headerRowOutlet;_footerRowOutlet;_noDataRowOutlet;_contentColumnDefs;_contentRowDefs;_contentHeaderRowDefs;_contentFooterRowDefs;_noDataRow;constructor(){s(new Te("role"),{optional:!0})||this._elementRef.nativeElement.setAttribute("role","table"),this._isServer=!this._platform.isBrowser,this._isNativeHtmlTable=this._elementRef.nativeElement.nodeName==="TABLE",this._dataDiffer=this._differs.find([]).create((t,i)=>this.trackBy?this.trackBy(i.dataIndex,i.data):i)}ngOnInit(){this._setupStickyStyler(),this._viewportRuler.change().pipe(ne(this._onDestroy)).subscribe(()=>{this._forceRecalculateCellWidths=!0})}ngAfterContentInit(){this._viewRepeater=this.recycleRows||this._virtualScrollEnabled()?new Ft:new At,this._virtualScrollEnabled()&&this._setupVirtualScrolling(this._virtualScrollViewport),this._hasInitialized=!0}ngAfterContentChecked(){this._canRender()&&this._render()}ngOnDestroy(){this._stickyStyler?.destroy(),[this._rowOutlet?.viewContainer,this._headerRowOutlet?.viewContainer,this._footerRowOutlet?.viewContainer,this._cachedRenderRowsMap,this._customColumnDefs,this._customRowDefs,this._customHeaderRowDefs,this._customFooterRowDefs,this._columnDefsByName].forEach(e=>{e?.clear()}),this._headerRowDefs=[],this._footerRowDefs=[],this._defaultRowDef=null,this._headerRowStickyUpdates.complete(),this._footerRowStickyUpdates.complete(),this._onDestroy.next(),this._onDestroy.complete(),Fe(this.dataSource)&&this.dataSource.disconnect(this)}renderRows(){this._renderRows=this._getAllRenderRows();let e=this._dataDiffer.diff(this._renderRows);if(!e){this._updateNoDataRow(),this.contentChanged.next();return}let t=this._rowOutlet.viewContainer;this._viewRepeater.applyChanges(e,t,(i,r,a)=>this._getEmbeddedViewArgs(i.item,a),i=>i.item.data,i=>{i.operation===It.INSERTED&&i.context&&this._renderCellTemplateForItem(i.record.item.rowDef,i.context)}),this._updateRowIndexContext(),e.forEachIdentityChange(i=>{let r=t.get(i.currentIndex);r.context.$implicit=i.item.data}),this._updateNoDataRow(),this.contentChanged.next(),this.updateStickyColumnStyles()}addColumnDef(e){this._customColumnDefs.add(e)}removeColumnDef(e){this._customColumnDefs.delete(e)}addRowDef(e){this._customRowDefs.add(e)}removeRowDef(e){this._customRowDefs.delete(e)}addHeaderRowDef(e){this._customHeaderRowDefs.add(e),this._headerRowDefChanged=!0}removeHeaderRowDef(e){this._customHeaderRowDefs.delete(e),this._headerRowDefChanged=!0}addFooterRowDef(e){this._customFooterRowDefs.add(e),this._footerRowDefChanged=!0}removeFooterRowDef(e){this._customFooterRowDefs.delete(e),this._footerRowDefChanged=!0}setNoDataRow(e){this._customNoDataRow=e}updateStickyHeaderRowStyles(){let e=this._getRenderedRows(this._headerRowOutlet);if(this._isNativeHtmlTable){let i=Xt(this._headerRowOutlet,"thead");i&&(i.style.display=e.length?"":"none")}let t=this._headerRowDefs.map(i=>i.sticky);this._stickyStyler.clearStickyPositioning(e,["top"]),this._stickyStyler.stickRows(e,t,"top"),this._headerRowDefs.forEach(i=>i.resetStickyChanged())}updateStickyFooterRowStyles(){let e=this._getRenderedRows(this._footerRowOutlet);if(this._isNativeHtmlTable){let i=Xt(this._footerRowOutlet,"tfoot");i&&(i.style.display=e.length?"":"none")}let t=this._footerRowDefs.map(i=>i.sticky);this._stickyStyler.clearStickyPositioning(e,["bottom"]),this._stickyStyler.stickRows(e,t,"bottom"),this._stickyStyler.updateStickyFooterContainer(this._elementRef.nativeElement,t),this._footerRowDefs.forEach(i=>i.resetStickyChanged())}updateStickyColumnStyles(){let e=this._getRenderedRows(this._headerRowOutlet),t=this._getRenderedRows(this._rowOutlet),i=this._getRenderedRows(this._footerRowOutlet);(this._isNativeHtmlTable&&!this.fixedLayout||this._stickyColumnStylesNeedReset)&&(this._stickyStyler.clearStickyPositioning([...e,...t,...i],["left","right"]),this._stickyColumnStylesNeedReset=!1),e.forEach((r,a)=>{this._addStickyColumnStyles([r],this._headerRowDefs[a])}),this._rowDefs.forEach(r=>{let a=[];for(let c=0;c<t.length;c++)this._renderRows[c].rowDef===r&&a.push(t[c]);this._addStickyColumnStyles(a,r)}),i.forEach((r,a)=>{this._addStickyColumnStyles([r],this._footerRowDefs[a])}),Array.from(this._columnDefsByName.values()).forEach(r=>r.resetStickyChanged())}stickyColumnsUpdated(e){this._positionListener?.stickyColumnsUpdated(e)}stickyEndColumnsUpdated(e){this._positionListener?.stickyEndColumnsUpdated(e)}stickyHeaderRowsUpdated(e){this._headerRowStickyUpdates.next(e),this._positionListener?.stickyHeaderRowsUpdated(e)}stickyFooterRowsUpdated(e){this._footerRowStickyUpdates.next(e),this._positionListener?.stickyFooterRowsUpdated(e)}_outletAssigned(){!this._hasAllOutlets&&this._rowOutlet&&this._headerRowOutlet&&this._footerRowOutlet&&this._noDataRowOutlet&&(this._hasAllOutlets=!0,this._canRender()&&this._render())}_canRender(){return this._hasAllOutlets&&this._hasInitialized}_render(){this._cacheRowDefs(),this._cacheColumnDefs(),!this._headerRowDefs.length&&!this._footerRowDefs.length&&this._rowDefs.length;let t=this._renderUpdatedColumns()||this._headerRowDefChanged||this._footerRowDefChanged;this._stickyColumnStylesNeedReset=this._stickyColumnStylesNeedReset||t,this._forceRecalculateCellWidths=t,this._headerRowDefChanged&&(this._forceRenderHeaderRows(),this._headerRowDefChanged=!1),this._footerRowDefChanged&&(this._forceRenderFooterRows(),this._footerRowDefChanged=!1),this.dataSource&&this._rowDefs.length>0&&!this._renderChangeSubscription?this._observeRenderChanges():this._stickyColumnStylesNeedReset&&this.updateStickyColumnStyles(),this._checkStickyStates()}_getAllRenderRows(){if(!Array.isArray(this._data)||!this._renderedRange)return[];let e=[],t=Math.min(this._data.length,this._renderedRange.end),i=this._cachedRenderRowsMap;this._cachedRenderRowsMap=new Map;for(let r=this._renderedRange.start;r<t;r++){let a=this._data[r],c=this._getRenderRowsForData(a,r,i.get(a));this._cachedRenderRowsMap.has(a)||this._cachedRenderRowsMap.set(a,new WeakMap);for(let l=0;l<c.length;l++){let h=c[l],_=this._cachedRenderRowsMap.get(h.data);_.has(h.rowDef)?_.get(h.rowDef).push(h):_.set(h.rowDef,[h]),e.push(h)}}return e}_getRenderRowsForData(e,t,i){return this._getRowDefs(e,t).map(a=>{let c=i&&i.has(a)?i.get(a):[];if(c.length){let l=c.shift();return l.dataIndex=t,l}else return{data:e,rowDef:a,dataIndex:t}})}_cacheColumnDefs(){this._columnDefsByName.clear(),Oe(this._getOwnDefs(this._contentColumnDefs),this._customColumnDefs).forEach(t=>{this._columnDefsByName.has(t.name),this._columnDefsByName.set(t.name,t)})}_cacheRowDefs(){this._headerRowDefs=Oe(this._getOwnDefs(this._contentHeaderRowDefs),this._customHeaderRowDefs),this._footerRowDefs=Oe(this._getOwnDefs(this._contentFooterRowDefs),this._customFooterRowDefs),this._rowDefs=Oe(this._getOwnDefs(this._contentRowDefs),this._customRowDefs);let e=this._rowDefs.filter(t=>!t.when);this._defaultRowDef=e[0]}_renderUpdatedColumns(){let e=(a,c)=>{let l=!!c.getColumnsDiff();return a||l},t=this._rowDefs.reduce(e,!1);t&&this._forceRenderDataRows();let i=this._headerRowDefs.reduce(e,!1);i&&this._forceRenderHeaderRows();let r=this._footerRowDefs.reduce(e,!1);return r&&this._forceRenderFooterRows(),t||i||r}_switchDataSource(e){this._data=[],Fe(this.dataSource)&&this.dataSource.disconnect(this),this._renderChangeSubscription&&(this._renderChangeSubscription.unsubscribe(),this._renderChangeSubscription=null),e||(this._dataDiffer&&this._dataDiffer.diff([]),this._rowOutlet&&this._rowOutlet.viewContainer.clear()),this._dataSource=e}_observeRenderChanges(){if(!this.dataSource)return;let e;Fe(this.dataSource)?e=this.dataSource.connect(this):dt(this.dataSource)?e=this.dataSource:Array.isArray(this.dataSource)&&(e=Be(this.dataSource)),this._renderChangeSubscription=Ce([e,this.viewChange]).pipe(ne(this._onDestroy)).subscribe(([t,i])=>{this._data=t||[],this._renderedRange=i,this._dataStream.next(t),this.renderRows()})}_forceRenderHeaderRows(){this._headerRowOutlet.viewContainer.length>0&&this._headerRowOutlet.viewContainer.clear(),this._headerRowDefs.forEach((e,t)=>this._renderRow(this._headerRowOutlet,e,t)),this.updateStickyHeaderRowStyles()}_forceRenderFooterRows(){this._footerRowOutlet.viewContainer.length>0&&this._footerRowOutlet.viewContainer.clear(),this._footerRowDefs.forEach((e,t)=>this._renderRow(this._footerRowOutlet,e,t)),this.updateStickyFooterRowStyles()}_addStickyColumnStyles(e,t){let i=Array.from(t?.columns||[]).map(c=>{let l=this._columnDefsByName.get(c);return l}),r=i.map(c=>c.sticky),a=i.map(c=>c.stickyEnd);this._stickyStyler.updateStickyColumns(e,r,a,!this.fixedLayout||this._forceRecalculateCellWidths)}_getRenderedRows(e){let t=[];for(let i=0;i<e.viewContainer.length;i++){let r=e.viewContainer.get(i);t.push(r.rootNodes[0])}return t}_getRowDefs(e,t){if(this._rowDefs.length===1)return[this._rowDefs[0]];let i=[];if(this.multiTemplateDataRows)i=this._rowDefs.filter(r=>!r.when||r.when(t,e));else{let r=this._rowDefs.find(a=>a.when&&a.when(t,e))||this._defaultRowDef;r&&i.push(r)}return i.length,i}_getEmbeddedViewArgs(e,t){let i=e.rowDef,r={$implicit:e.data};return{templateRef:i.template,context:r,index:t}}_renderRow(e,t,i,r={}){let a=e.viewContainer.createEmbeddedView(t.template,r,i);return this._renderCellTemplateForItem(t,r),a}_renderCellTemplateForItem(e,t){for(let i of this._getCellTemplates(e))re.mostRecentCellOutlet&&re.mostRecentCellOutlet._viewContainer.createEmbeddedView(i,t);this._changeDetectorRef.markForCheck()}_updateRowIndexContext(){let e=this._rowOutlet.viewContainer;for(let t=0,i=e.length;t<i;t++){let a=e.get(t).context;a.count=i,a.first=t===0,a.last=t===i-1,a.even=t%2===0,a.odd=!a.even,this.multiTemplateDataRows?(a.dataIndex=this._renderRows[t].dataIndex,a.renderIndex=t):a.index=this._renderRows[t].dataIndex}}_getCellTemplates(e){return!e||!e.columns?[]:Array.from(e.columns,t=>{let i=this._columnDefsByName.get(t);return e.extractCellTemplate(i)})}_forceRenderDataRows(){this._dataDiffer.diff([]),this._rowOutlet.viewContainer.clear(),this.renderRows()}_checkStickyStates(){let e=(t,i)=>t||i.hasStickyChanged();this._headerRowDefs.reduce(e,!1)&&this.updateStickyHeaderRowStyles(),this._footerRowDefs.reduce(e,!1)&&this.updateStickyFooterRowStyles(),Array.from(this._columnDefsByName.values()).reduce(e,!1)&&(this._stickyColumnStylesNeedReset=!0,this.updateStickyColumnStyles())}_setupStickyStyler(){let e=this._dir?this._dir.value:"ltr",t=this._injector;this._stickyStyler=new Ve(this._isNativeHtmlTable,this.stickyCssClass,this._platform.isBrowser,this.needsPositionStickyOnElement,e,this,t),(this._dir?this._dir.change:Be()).pipe(ne(this._onDestroy)).subscribe(i=>{this._stickyStyler.direction=i,this.updateStickyColumnStyles()})}_setupVirtualScrolling(e){let t=typeof requestAnimationFrame<"u"?lt:ct;this.viewChange.next({start:0,end:0}),e.renderedRangeStream.pipe(ht(0,t),ne(this._onDestroy)).subscribe(this.viewChange),e.attach({dataStream:this._dataStream,measureRangeSize:(i,r)=>this._measureRangeSize(i,r)}),Ce([e.renderedContentOffset,this._headerRowStickyUpdates]).pipe(ne(this._onDestroy)).subscribe(([i,r])=>{if(!(!r.sizes||!r.offsets||!r.elements))for(let a=0;a<r.elements.length;a++){let c=r.elements[a];if(c){let l=r.offsets[a],h=i!==0?Math.max(i-l,l):-l;for(let _ of c)_.style.top=`${-h}px`}}}),Ce([e.renderedContentOffset,this._footerRowStickyUpdates]).pipe(ne(this._onDestroy)).subscribe(([i,r])=>{if(!(!r.sizes||!r.offsets||!r.elements))for(let a=0;a<r.elements.length;a++){let c=r.elements[a];if(c)for(let l of c)l.style.bottom=`${i+r.offsets[a]}px`}})}_getOwnDefs(e){return e.filter(t=>!t._table||t._table===this)}_updateNoDataRow(){let e=this._customNoDataRow||this._noDataRow;if(!e)return;let t=this._rowOutlet.viewContainer.length===0;if(t===this._isShowingNoDataRow)return;let i=this._noDataRowOutlet.viewContainer;if(t){let r=i.createEmbeddedView(e.templateRef),a=r.rootNodes[0];if(r.rootNodes.length===1&&a?.nodeType===this._document.ELEMENT_NODE){a.setAttribute("role","row"),a.classList.add(...e._contentClassNames);let c=a.querySelectorAll(e._cellSelector);for(let l=0;l<c.length;l++)c[l].classList.add(...e._cellClassNames)}}else i.clear();this._isShowingNoDataRow=t,this._changeDetectorRef.markForCheck()}_measureRangeSize(e,t){if(e.start>=e.end||t!=="vertical")return 0;let i=this.viewChange.value,r=this._rowOutlet.viewContainer;e.start<i.start||e.end>i.end;let a=e.start-i.start,c=e.end-e.start,l,h;for(let g=0;g<c;g++){let k=r.get(g+a);if(k&&k.rootNodes.length){l=h=k.rootNodes[0];break}}for(let g=c-1;g>-1;g--){let k=r.get(g+a);if(k&&k.rootNodes.length){h=k.rootNodes[k.rootNodes.length-1];break}}let _=l?.getBoundingClientRect?.(),O=h?.getBoundingClientRect?.();return _&&O?O.bottom-_.top:0}_virtualScrollEnabled(){return!this._disableVirtualScrolling&&this._virtualScrollViewport!=null}static \u0275fac=function(t){return new(t||n)};static \u0275cmp=v({type:n,selectors:[["cdk-table"],["table","cdk-table",""]],contentQueries:function(t,i,r){if(t&1&&fe(r,Wt,5)(r,te,5)(r,Pe,5)(r,ve,5)(r,Xe,5),t&2){let a;T(a=M())&&(i._noDataRow=a.first),T(a=M())&&(i._contentColumnDefs=a),T(a=M())&&(i._contentRowDefs=a),T(a=M())&&(i._contentHeaderRowDefs=a),T(a=M())&&(i._contentFooterRowDefs=a)}},hostAttrs:[1,"cdk-table"],hostVars:2,hostBindings:function(t,i){t&2&&N("cdk-table-fixed-layout",i.fixedLayout)},inputs:{trackBy:"trackBy",dataSource:"dataSource",multiTemplateDataRows:[2,"multiTemplateDataRows","multiTemplateDataRows",p],fixedLayout:[2,"fixedLayout","fixedLayout",p],recycleRows:[2,"recycleRows","recycleRows",p]},outputs:{contentChanged:"contentChanged"},exportAs:["cdkTable"],features:[F([{provide:H,useExisting:n},{provide:ke,useValue:null}])],ngContentSelectors:bn,decls:5,vars:2,consts:[["role","rowgroup"],["headerRowOutlet",""],["rowOutlet",""],["noDataRowOutlet",""],["footerRowOutlet",""]],template:function(t,i){t&1&&(Z(_n),I(0),I(1,1),C(2,gn,1,0),C(3,kn,7,0)(4,yn,4,0)),t&2&&(d(2),w(i._isServer?2:-1),d(),w(i._isNativeHtmlTable?3:4))},dependencies:[We,Qe,Ke,Ge],styles:[`.cdk-table-fixed-layout {
  table-layout: fixed;
}
`],encapsulation:2})}return n})();function Oe(n,o){return n.concat(Array.from(o))}function Xt(n,o){let e=o.toUpperCase(),t=n.viewContainer.element.nativeElement;for(;t;){let i=t.nodeType===1?t.nodeName:null;if(i===e)return t;if(i==="TABLE")break;t=t.parentNode}return null}var Gt=(()=>{class n{static \u0275fac=function(t){return new(t||n)};static \u0275mod=U({type:n});static \u0275inj=L({imports:[Pt]})}return n})();var Cn=[[["caption"]],[["colgroup"],["col"]],"*"],wn=["caption","colgroup, col","*"];function xn(n,o){n&1&&I(0,2)}function Dn(n,o){n&1&&(u(0,"thead",0),D(1,1),f(),u(2,"tbody",2),D(3,3)(4,4),f(),u(5,"tfoot",0),D(6,5),f())}function Rn(n,o){n&1&&D(0,1)(1,3)(2,4)(3,5)}var Kt=(()=>{class n extends Ze{stickyCssClass="mat-mdc-table-sticky";needsPositionStickyOnElement=!1;static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275cmp=v({type:n,selectors:[["mat-table"],["table","mat-table",""]],hostAttrs:[1,"mat-mdc-table","mdc-data-table__table"],hostVars:2,hostBindings:function(t,i){t&2&&N("mat-table-fixed-layout",i.fixedLayout)},exportAs:["matTable"],features:[F([{provide:Ze,useExisting:n},{provide:H,useExisting:n},{provide:ke,useValue:null}]),y],ngContentSelectors:wn,decls:5,vars:2,consts:[["role","rowgroup"],["headerRowOutlet",""],["role","rowgroup",1,"mdc-data-table__content"],["rowOutlet",""],["noDataRowOutlet",""],["footerRowOutlet",""]],template:function(t,i){t&1&&(Z(Cn),I(0),I(1,1),C(2,xn,1,0),C(3,Dn,7,0)(4,Rn,4,0)),t&2&&(d(2),w(i._isServer?2:-1),d(),w(i._isNativeHtmlTable?3:4))},dependencies:[We,Qe,Ke,Ge],styles:[`.mat-mdc-table-sticky {
  position: sticky !important;
}

mat-table {
  display: block;
}

mat-header-row {
  min-height: var(--mat-table-header-container-height, 56px);
}

mat-row {
  min-height: var(--mat-table-row-item-container-height, 52px);
}

mat-footer-row {
  min-height: var(--mat-table-footer-container-height, 52px);
}

mat-row, mat-header-row, mat-footer-row {
  display: flex;
  border-width: 0;
  border-bottom-width: 1px;
  border-style: solid;
  align-items: center;
  box-sizing: border-box;
}

mat-cell:first-of-type, mat-header-cell:first-of-type, mat-footer-cell:first-of-type {
  padding-left: 24px;
}
[dir=rtl] mat-cell:first-of-type:not(:only-of-type), [dir=rtl] mat-header-cell:first-of-type:not(:only-of-type), [dir=rtl] mat-footer-cell:first-of-type:not(:only-of-type) {
  padding-left: 0;
  padding-right: 24px;
}
mat-cell:last-of-type, mat-header-cell:last-of-type, mat-footer-cell:last-of-type {
  padding-right: 24px;
}
[dir=rtl] mat-cell:last-of-type:not(:only-of-type), [dir=rtl] mat-header-cell:last-of-type:not(:only-of-type), [dir=rtl] mat-footer-cell:last-of-type:not(:only-of-type) {
  padding-right: 0;
  padding-left: 24px;
}

mat-cell, mat-header-cell, mat-footer-cell {
  flex: 1;
  display: flex;
  align-items: center;
  overflow: hidden;
  word-wrap: break-word;
  min-height: inherit;
}

.mat-mdc-table {
  min-width: 100%;
  border: 0;
  border-spacing: 0;
  table-layout: auto;
  white-space: normal;
  background-color: var(--mat-table-background-color, var(--mat-sys-surface));
}

.mat-table-fixed-layout {
  table-layout: fixed;
}

.mdc-data-table__cell {
  box-sizing: border-box;
  overflow: hidden;
  text-align: start;
  text-overflow: ellipsis;
}

.mdc-data-table__cell,
.mdc-data-table__header-cell {
  padding: 0 16px;
}

.mat-mdc-header-row {
  -moz-osx-font-smoothing: grayscale;
  -webkit-font-smoothing: antialiased;
  height: var(--mat-table-header-container-height, 56px);
  color: var(--mat-table-header-headline-color, var(--mat-sys-on-surface, rgba(0, 0, 0, 0.87)));
  font-family: var(--mat-table-header-headline-font, var(--mat-sys-title-small-font, Roboto, sans-serif));
  line-height: var(--mat-table-header-headline-line-height, var(--mat-sys-title-small-line-height));
  font-size: var(--mat-table-header-headline-size, var(--mat-sys-title-small-size, 14px));
  font-weight: var(--mat-table-header-headline-weight, var(--mat-sys-title-small-weight, 500));
}

.mat-mdc-row {
  height: var(--mat-table-row-item-container-height, 52px);
  color: var(--mat-table-row-item-label-text-color, var(--mat-sys-on-surface, rgba(0, 0, 0, 0.87)));
}

.mat-mdc-row,
.mdc-data-table__content {
  -moz-osx-font-smoothing: grayscale;
  -webkit-font-smoothing: antialiased;
  font-family: var(--mat-table-row-item-label-text-font, var(--mat-sys-body-medium-font, Roboto, sans-serif));
  line-height: var(--mat-table-row-item-label-text-line-height, var(--mat-sys-body-medium-line-height));
  font-size: var(--mat-table-row-item-label-text-size, var(--mat-sys-body-medium-size, 14px));
  font-weight: var(--mat-table-row-item-label-text-weight, var(--mat-sys-body-medium-weight));
}

.mat-mdc-footer-row {
  -moz-osx-font-smoothing: grayscale;
  -webkit-font-smoothing: antialiased;
  height: var(--mat-table-footer-container-height, 52px);
  color: var(--mat-table-row-item-label-text-color, var(--mat-sys-on-surface, rgba(0, 0, 0, 0.87)));
  font-family: var(--mat-table-footer-supporting-text-font, var(--mat-sys-body-medium-font, Roboto, sans-serif));
  line-height: var(--mat-table-footer-supporting-text-line-height, var(--mat-sys-body-medium-line-height));
  font-size: var(--mat-table-footer-supporting-text-size, var(--mat-sys-body-medium-size, 14px));
  font-weight: var(--mat-table-footer-supporting-text-weight, var(--mat-sys-body-medium-weight));
  letter-spacing: var(--mat-table-footer-supporting-text-tracking, var(--mat-sys-body-medium-tracking));
}

.mat-mdc-header-cell {
  border-bottom-color: var(--mat-table-row-item-outline-color, var(--mat-sys-outline, rgba(0, 0, 0, 0.12)));
  border-bottom-width: var(--mat-table-row-item-outline-width, 1px);
  border-bottom-style: solid;
  letter-spacing: var(--mat-table-header-headline-tracking, var(--mat-sys-title-small-tracking));
  font-weight: inherit;
  line-height: inherit;
  box-sizing: border-box;
  text-overflow: ellipsis;
  overflow: hidden;
  outline: none;
  text-align: start;
}
.mdc-data-table__row:last-child > .mat-mdc-header-cell {
  border-bottom: none;
}

.mat-mdc-cell {
  border-bottom-color: var(--mat-table-row-item-outline-color, var(--mat-sys-outline, rgba(0, 0, 0, 0.12)));
  border-bottom-width: var(--mat-table-row-item-outline-width, 1px);
  border-bottom-style: solid;
  letter-spacing: var(--mat-table-row-item-label-text-tracking, var(--mat-sys-body-medium-tracking));
  line-height: inherit;
}
.mdc-data-table__row:last-child > .mat-mdc-cell {
  border-bottom: none;
}

.mat-mdc-footer-cell {
  letter-spacing: var(--mat-table-row-item-label-text-tracking, var(--mat-sys-body-medium-tracking));
}

mat-row.mat-mdc-row,
mat-header-row.mat-mdc-header-row,
mat-footer-row.mat-mdc-footer-row {
  border-bottom: none;
}

.mat-mdc-table tbody,
.mat-mdc-table tfoot,
.mat-mdc-table thead,
.mat-mdc-cell,
.mat-mdc-footer-cell,
.mat-mdc-header-row,
.mat-mdc-row,
.mat-mdc-footer-row,
.mat-mdc-table .mat-mdc-header-cell {
  background: inherit;
}

.mat-mdc-table mat-header-row.mat-mdc-header-row,
.mat-mdc-table mat-row.mat-mdc-row,
.mat-mdc-table mat-footer-row.mat-mdc-footer-cell {
  height: unset;
}

mat-header-cell.mat-mdc-header-cell,
mat-cell.mat-mdc-cell,
mat-footer-cell.mat-mdc-footer-cell {
  align-self: stretch;
}
`],encapsulation:2})}return n})(),Zt=(()=>{class n extends Ne{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275dir=m({type:n,selectors:[["","matCellDef",""]],features:[F([{provide:Ne,useExisting:n}]),y]})}return n})(),Yt=(()=>{class n extends ze{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275dir=m({type:n,selectors:[["","matHeaderCellDef",""]],features:[F([{provide:ze,useExisting:n}]),y]})}return n})();var Jt=(()=>{class n extends te{get name(){return this._name}set name(e){this._setNameInput(e)}_updateColumnCssClassName(){super._updateColumnCssClassName(),this._columnCssClassName.push(`mat-column-${this.cssClassFriendlyName}`)}static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275dir=m({type:n,selectors:[["","matColumnDef",""]],inputs:{name:[0,"matColumnDef","name"]},features:[F([{provide:te,useExisting:n}]),y]})}return n})(),en=(()=>{class n extends $t{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275dir=m({type:n,selectors:[["mat-header-cell"],["th","mat-header-cell",""]],hostAttrs:["role","columnheader",1,"mat-mdc-header-cell","mdc-data-table__header-cell"],features:[y]})}return n})();var tn=(()=>{class n extends Qt{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275dir=m({type:n,selectors:[["mat-cell"],["td","mat-cell",""]],hostAttrs:[1,"mat-mdc-cell","mdc-data-table__cell"],features:[y]})}return n})();var nn=(()=>{class n extends ve{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275dir=m({type:n,selectors:[["","matHeaderRowDef",""]],inputs:{columns:[0,"matHeaderRowDef","columns"],sticky:[2,"matHeaderRowDefSticky","sticky",p]},features:[F([{provide:ve,useExisting:n}]),y]})}return n})();var rn=(()=>{class n extends Pe{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275dir=m({type:n,selectors:[["","matRowDef",""]],inputs:{columns:[0,"matRowDefColumns","columns"],when:[0,"matRowDefWhen","when"]},features:[F([{provide:Pe,useExisting:n}]),y]})}return n})(),on=(()=>{class n extends qe{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275cmp=v({type:n,selectors:[["mat-header-row"],["tr","mat-header-row",""]],hostAttrs:["role","row",1,"mat-mdc-header-row","mdc-data-table__header-row"],exportAs:["matHeaderRow"],features:[F([{provide:qe,useExisting:n}]),y],decls:1,vars:0,consts:[["cdkCellOutlet",""]],template:function(t,i){t&1&&D(0,0)},dependencies:[re],encapsulation:2})}return n})();var an=(()=>{class n extends $e{static \u0275fac=(()=>{let e;return function(i){return(e||(e=P(n)))(i||n)}})();static \u0275cmp=v({type:n,selectors:[["mat-row"],["tr","mat-row",""]],hostAttrs:["role","row",1,"mat-mdc-row","mdc-data-table__row"],exportAs:["matRow"],features:[F([{provide:$e,useExisting:n}]),y],decls:1,vars:0,consts:[["cdkCellOutlet",""]],template:function(t,i){t&1&&D(0,0)},dependencies:[re],encapsulation:2})}return n})();var sn=(()=>{class n{static \u0275fac=function(t){return new(t||n)};static \u0275mod=U({type:n});static \u0275inj=L({imports:[Gt,ee]})}return n})();var Tn=["mat-sort-header",""],Mn=["*",[["","matSortHeaderIcon",""]]],En=["*","[matSortHeaderIcon]"];function In(n,o){n&1&&(we(),K(0,"svg",3),ie(1,"path",4),$())}function Fn(n,o){n&1&&(K(0,"div",2),I(1,1,null,In,2,0),$())}var cn=new j("MAT_SORT_DEFAULT_OPTIONS"),Ye=(()=>{class n{_defaultOptions;_initializedStream=new st(1);sortables=new Map;_stateChanges=new Q;active;start="asc";get direction(){return this._direction}set direction(e){this._direction=e}_direction="";disableClear;disabled=!1;sortChange=new X;initialized=this._initializedStream;constructor(e){this._defaultOptions=e}register(e){this.sortables.set(e.id,e)}deregister(e){this.sortables.delete(e.id)}sort(e){this.active!=e.id?(this.active=e.id,this.direction=e.start?e.start:this.start):this.direction=this.getNextSortDirection(e),this.sortChange.emit({active:this.active,direction:this.direction})}getNextSortDirection(e){if(!e)return"";let t=e?.disableClear??this.disableClear??!!this._defaultOptions?.disableClear,i=On(e.start||this.start,t),r=i.indexOf(this.direction)+1;return r>=i.length&&(r=0),i[r]}ngOnInit(){this._initializedStream.next()}ngOnChanges(){this._stateChanges.next()}ngOnDestroy(){this._stateChanges.complete(),this._initializedStream.complete()}static \u0275fac=function(t){return new(t||n)(Re(cn,8))};static \u0275dir=m({type:n,selectors:[["","matSort",""]],hostAttrs:[1,"mat-sort"],inputs:{active:[0,"matSortActive","active"],start:[0,"matSortStart","start"],direction:[0,"matSortDirection","direction"],disableClear:[2,"matSortDisableClear","disableClear",p],disabled:[2,"matSortDisabled","disabled",p]},outputs:{sortChange:"matSortChange"},exportAs:["matSort"],features:[W]})}return n})();function On(n,o){let e=["asc","desc"];return n=="desc"&&e.reverse(),o||e.push(""),e}var ln=(()=>{class n{_sort=s(Ye,{optional:!0});_columnDef=s(te,{optional:!0});_changeDetectorRef=s(J);_focusMonitor=s(Dt);_elementRef=s(S);_ariaDescriber=s(St,{optional:!0});_renderChanges;_animationsDisabled=Me();_recentlyCleared=De(null);_sortButton;id;arrowPosition="after";start;disabled=!1;get sortActionDescription(){return this._sortActionDescription}set sortActionDescription(e){this._updateSortActionDescription(e)}_sortActionDescription="Sort";disableClear;constructor(){s(Ee).load(Ie);let e=s(cn,{optional:!0});this._sort,e?.arrowPosition&&(this.arrowPosition=e?.arrowPosition)}ngOnInit(){!this.id&&this._columnDef&&(this.id=this._columnDef.name),this._sort.register(this),this._renderChanges=mt(this._sort._stateChanges,this._sort.sortChange).subscribe(()=>this._changeDetectorRef.markForCheck()),this._sortButton=this._elementRef.nativeElement.querySelector(".mat-sort-header-container"),this._updateSortActionDescription(this._sortActionDescription)}ngAfterViewInit(){this._focusMonitor.monitor(this._elementRef,!0).subscribe(()=>{Promise.resolve().then(()=>this._recentlyCleared.set(null))})}ngOnDestroy(){this._focusMonitor.stopMonitoring(this._elementRef),this._sort.deregister(this),this._renderChanges?.unsubscribe(),this._sortButton&&this._ariaDescriber?.removeDescription(this._sortButton,this._sortActionDescription)}_toggleOnInteraction(){if(!this._isDisabled()){let e=this._isSorted(),t=this._sort.direction;this._sort.sort(this),this._recentlyCleared.set(e&&!this._isSorted()?t:null)}}_handleKeydown(e){(e.keyCode===32||e.keyCode===13)&&(e.preventDefault(),this._toggleOnInteraction())}_isSorted(){return this._sort.active==this.id&&(this._sort.direction==="asc"||this._sort.direction==="desc")}_isDisabled(){return this._sort.disabled||this.disabled}_getAriaSortAttribute(){return this._isSorted()?this._sort.direction=="asc"?"ascending":"descending":"none"}_renderArrow(){return!this._isDisabled()||this._isSorted()}_updateSortActionDescription(e){this._sortButton&&(this._ariaDescriber?.removeDescription(this._sortButton,this._sortActionDescription),this._ariaDescriber?.describe(this._sortButton,e)),this._sortActionDescription=e}static \u0275fac=function(t){return new(t||n)};static \u0275cmp=v({type:n,selectors:[["","mat-sort-header",""]],hostAttrs:[1,"mat-sort-header"],hostVars:3,hostBindings:function(t,i){t&1&&A("click",function(){return i._toggleOnInteraction()})("keydown",function(a){return i._handleKeydown(a)})("mouseleave",function(){return i._recentlyCleared.set(null)}),t&2&&(G("aria-sort",i._getAriaSortAttribute()),N("mat-sort-header-disabled",i._isDisabled()))},inputs:{id:[0,"mat-sort-header","id"],arrowPosition:"arrowPosition",start:"start",disabled:[2,"disabled","disabled",p],sortActionDescription:"sortActionDescription",disableClear:[2,"disableClear","disableClear",p]},exportAs:["matSortHeader"],attrs:Tn,ngContentSelectors:En,decls:4,vars:17,consts:[[1,"mat-sort-header-container","mat-focus-indicator"],[1,"mat-sort-header-content"],[1,"mat-sort-header-arrow"],["viewBox","0 -960 960 960","focusable","false","aria-hidden","true"],["d","M440-240v-368L296-464l-56-56 240-240 240 240-56 56-144-144v368h-80Z"]],template:function(t,i){t&1&&(Z(Mn),K(0,"div",0)(1,"div",1),I(2),$(),C(3,Fn,3,0,"div",2),$()),t&2&&(N("mat-sort-header-sorted",i._isSorted())("mat-sort-header-position-before",i.arrowPosition==="before")("mat-sort-header-descending",i._sort.direction==="desc")("mat-sort-header-ascending",i._sort.direction==="asc")("mat-sort-header-recently-cleared-ascending",i._recentlyCleared()==="asc")("mat-sort-header-recently-cleared-descending",i._recentlyCleared()==="desc")("mat-sort-header-animations-disabled",i._animationsDisabled),G("tabindex",i._isDisabled()?null:0)("role",i._isDisabled()?null:"button"),d(3),w(i._renderArrow()?3:-1))},styles:[`.mat-sort-header {
  cursor: pointer;
}

.mat-sort-header-disabled {
  cursor: default;
}

.mat-sort-header-container {
  display: flex;
  align-items: center;
  letter-spacing: normal;
  outline: 0;
}
[mat-sort-header].cdk-keyboard-focused .mat-sort-header-container, [mat-sort-header].cdk-program-focused .mat-sort-header-container {
  border-bottom: solid 1px currentColor;
}
.mat-sort-header-container::before {
  margin: calc(calc(var(--mat-focus-indicator-border-width, 3px) + 2px) * -1);
}

.mat-sort-header-content {
  display: flex;
  align-items: center;
}

.mat-sort-header-position-before {
  flex-direction: row-reverse;
}

@keyframes _mat-sort-header-recently-cleared-ascending {
  from {
    transform: translateY(0);
    opacity: 1;
  }
  to {
    transform: translateY(-25%);
    opacity: 0;
  }
}
@keyframes _mat-sort-header-recently-cleared-descending {
  from {
    transform: translateY(0) rotate(180deg);
    opacity: 1;
  }
  to {
    transform: translateY(25%) rotate(180deg);
    opacity: 0;
  }
}
.mat-sort-header-arrow {
  height: 12px;
  width: 12px;
  position: relative;
  transition: transform 225ms cubic-bezier(0.4, 0, 0.2, 1), opacity 225ms cubic-bezier(0.4, 0, 0.2, 1);
  opacity: 0;
  overflow: visible;
  color: var(--mat-sort-arrow-color, var(--mat-sys-on-surface));
}
.mat-sort-header.cdk-keyboard-focused .mat-sort-header-arrow, .mat-sort-header.cdk-program-focused .mat-sort-header-arrow, .mat-sort-header:hover .mat-sort-header-arrow {
  opacity: 0.54;
}
.mat-sort-header .mat-sort-header-sorted .mat-sort-header-arrow {
  opacity: 1;
}
.mat-sort-header-descending .mat-sort-header-arrow {
  transform: rotate(180deg);
}
.mat-sort-header-recently-cleared-ascending .mat-sort-header-arrow {
  transform: translateY(-25%);
}
.mat-sort-header-recently-cleared-ascending .mat-sort-header-arrow {
  transition: none;
  animation: _mat-sort-header-recently-cleared-ascending 225ms cubic-bezier(0.4, 0, 0.2, 1) forwards;
}
.mat-sort-header-recently-cleared-descending .mat-sort-header-arrow {
  transition: none;
  animation: _mat-sort-header-recently-cleared-descending 225ms cubic-bezier(0.4, 0, 0.2, 1) forwards;
}
.mat-sort-header-animations-disabled .mat-sort-header-arrow {
  transition-duration: 0ms;
  animation-duration: 0ms;
}
.mat-sort-header-arrow > svg, .mat-sort-header-arrow [matSortHeaderIcon] {
  width: 24px;
  height: 24px;
  fill: currentColor;
  position: absolute;
  top: 50%;
  left: 50%;
  margin: -12px 0 0 -12px;
  transform: translateZ(0);
}
.mat-sort-header-arrow, [dir=rtl] .mat-sort-header-position-before .mat-sort-header-arrow {
  margin: 0 0 0 6px;
}
.mat-sort-header-position-before .mat-sort-header-arrow, [dir=rtl] .mat-sort-header-arrow {
  margin: 0 6px 0 0;
}
`],encapsulation:2,changeDetection:0})}return n})(),dn=(()=>{class n{static \u0275fac=function(t){return new(t||n)};static \u0275mod=U({type:n});static \u0275inj=L({imports:[ee]})}return n})();var Nn=["input"],zn=["label"],Pn=["*"],Je={color:"accent",clickAction:"check-indeterminate",disabledInteractive:!1},Bn=new j("mat-checkbox-default-options",{providedIn:"root",factory:()=>Je}),R=(function(n){return n[n.Init=0]="Init",n[n.Checked=1]="Checked",n[n.Unchecked=2]="Unchecked",n[n.Indeterminate=3]="Indeterminate",n})(R||{}),et=class{source;checked},tt=(()=>{class n{_elementRef=s(S);_changeDetectorRef=s(J);_ngZone=s(xe);_animationsDisabled=Me();_options=s(Bn,{optional:!0});focus(){this._inputElement.nativeElement.focus()}_createChangeEvent(e){let t=new et;return t.source=this,t.checked=e,t}_getAnimationTargetElement(){return this._inputElement?.nativeElement}_animationClasses={uncheckedToChecked:"mdc-checkbox--anim-unchecked-checked",uncheckedToIndeterminate:"mdc-checkbox--anim-unchecked-indeterminate",checkedToUnchecked:"mdc-checkbox--anim-checked-unchecked",checkedToIndeterminate:"mdc-checkbox--anim-checked-indeterminate",indeterminateToChecked:"mdc-checkbox--anim-indeterminate-checked",indeterminateToUnchecked:"mdc-checkbox--anim-indeterminate-unchecked"};ariaLabel="";ariaLabelledby=null;ariaDescribedby;ariaExpanded;ariaControls;ariaOwns;_uniqueId;id;get inputId(){return`${this.id||this._uniqueId}-input`}required=!1;labelPosition="after";name=null;change=new X;indeterminateChange=new X;value;disableRipple=!1;_inputElement;_labelElement;tabIndex;color;disabledInteractive;_onTouched=()=>{};_currentAnimationClass="";_currentCheckState=R.Init;_controlValueAccessorChangeFn=()=>{};_validatorChangeFn=()=>{};constructor(){s(Ee).load(Ie);let e=s(new Te("tabindex"),{optional:!0});this._options=this._options||Je,this.color=this._options.color||Je.color,this.tabIndex=e==null?0:parseInt(e)||0,this.id=this._uniqueId=s(Rt).getId("mat-mdc-checkbox-"),this.disabledInteractive=this._options?.disabledInteractive??!1}ngOnChanges(e){e.required&&this._validatorChangeFn()}ngAfterViewInit(){this._syncIndeterminate(this.indeterminate)}get checked(){return this._checked}set checked(e){e!=this.checked&&(this._checked=e,this._changeDetectorRef.markForCheck())}_checked=!1;get disabled(){return this._disabled}set disabled(e){e!==this.disabled&&(this._disabled=e,this._changeDetectorRef.markForCheck())}_disabled=!1;get indeterminate(){return this._indeterminate()}set indeterminate(e){let t=e!=this._indeterminate();this._indeterminate.set(e),t&&(e?this._transitionCheckState(R.Indeterminate):this._transitionCheckState(this.checked?R.Checked:R.Unchecked),this.indeterminateChange.emit(e)),this._syncIndeterminate(e)}_indeterminate=De(!1);_isRippleDisabled(){return this.disableRipple||this.disabled}_onLabelTextChange(){this._changeDetectorRef.detectChanges()}writeValue(e){this.checked=!!e}registerOnChange(e){this._controlValueAccessorChangeFn=e}registerOnTouched(e){this._onTouched=e}setDisabledState(e){this.disabled=e}validate(e){return this.required&&e.value!==!0?{required:!0}:null}registerOnValidatorChange(e){this._validatorChangeFn=e}_transitionCheckState(e){let t=this._currentCheckState,i=this._getAnimationTargetElement();if(!(t===e||!i)&&(this._currentAnimationClass&&i.classList.remove(this._currentAnimationClass),this._currentAnimationClass=this._getAnimationClassForCheckStateTransition(t,e),this._currentCheckState=e,this._currentAnimationClass.length>0)){i.classList.add(this._currentAnimationClass);let r=this._currentAnimationClass;this._ngZone.runOutsideAngular(()=>{setTimeout(()=>{i.classList.remove(r)},1e3)})}}_emitChangeEvent(){this._controlValueAccessorChangeFn(this.checked),this.change.emit(this._createChangeEvent(this.checked)),this._inputElement&&(this._inputElement.nativeElement.checked=this.checked)}toggle(){this.checked=!this.checked,this._controlValueAccessorChangeFn(this.checked)}_handleInputClick(){let e=this._options?.clickAction;!this.disabled&&e!=="noop"?(this.indeterminate&&e!=="check"&&Promise.resolve().then(()=>{this._indeterminate.set(!1),this.indeterminateChange.emit(!1)}),this._checked=!this._checked,this._transitionCheckState(this._checked?R.Checked:R.Unchecked),this._emitChangeEvent()):(this.disabled&&this.disabledInteractive||!this.disabled&&e==="noop")&&(this._inputElement.nativeElement.checked=this.checked,this._inputElement.nativeElement.indeterminate=this.indeterminate)}_onInteractionEvent(e){e.stopPropagation()}_onBlur(){Promise.resolve().then(()=>{this._onTouched(),this._changeDetectorRef.markForCheck()})}_getAnimationClassForCheckStateTransition(e,t){if(this._animationsDisabled)return"";switch(e){case R.Init:if(t===R.Checked)return this._animationClasses.uncheckedToChecked;if(t==R.Indeterminate)return this._checked?this._animationClasses.checkedToIndeterminate:this._animationClasses.uncheckedToIndeterminate;break;case R.Unchecked:return t===R.Checked?this._animationClasses.uncheckedToChecked:this._animationClasses.uncheckedToIndeterminate;case R.Checked:return t===R.Unchecked?this._animationClasses.checkedToUnchecked:this._animationClasses.checkedToIndeterminate;case R.Indeterminate:return t===R.Checked?this._animationClasses.indeterminateToChecked:this._animationClasses.indeterminateToUnchecked}return""}_syncIndeterminate(e){let t=this._inputElement;t&&(t.nativeElement.indeterminate=e)}_onInputClick(){this._handleInputClick()}_onTouchTargetClick(){this._handleInputClick(),this.disabled||this._inputElement.nativeElement.focus()}_preventBubblingFromLabel(e){e.target&&this._labelElement.nativeElement.contains(e.target)&&e.stopPropagation()}static \u0275fac=function(t){return new(t||n)};static \u0275cmp=v({type:n,selectors:[["mat-checkbox"]],viewQuery:function(t,i){if(t&1&&Le(Nn,5)(zn,5),t&2){let r;T(r=M())&&(i._inputElement=r.first),T(r=M())&&(i._labelElement=r.first)}},hostAttrs:[1,"mat-mdc-checkbox"],hostVars:16,hostBindings:function(t,i){t&2&&(kt("id",i.id),G("tabindex",null)("aria-label",null)("aria-labelledby",null),Se(i.color?"mat-"+i.color:"mat-accent"),N("_mat-animation-noopable",i._animationsDisabled)("mdc-checkbox--disabled",i.disabled)("mat-mdc-checkbox-disabled",i.disabled)("mat-mdc-checkbox-checked",i.checked)("mat-mdc-checkbox-disabled-interactive",i.disabledInteractive))},inputs:{ariaLabel:[0,"aria-label","ariaLabel"],ariaLabelledby:[0,"aria-labelledby","ariaLabelledby"],ariaDescribedby:[0,"aria-describedby","ariaDescribedby"],ariaExpanded:[2,"aria-expanded","ariaExpanded",p],ariaControls:[0,"aria-controls","ariaControls"],ariaOwns:[0,"aria-owns","ariaOwns"],id:"id",required:[2,"required","required",p],labelPosition:"labelPosition",name:"name",value:"value",disableRipple:[2,"disableRipple","disableRipple",p],tabIndex:[2,"tabIndex","tabIndex",e=>e==null?void 0:ge(e)],color:"color",disabledInteractive:[2,"disabledInteractive","disabledInteractive",p],checked:[2,"checked","checked",p],disabled:[2,"disabled","disabled",p],indeterminate:[2,"indeterminate","indeterminate",p]},outputs:{change:"change",indeterminateChange:"indeterminateChange"},exportAs:["matCheckbox"],features:[F([{provide:Bt,useExisting:ut(()=>n),multi:!0},{provide:Ht,useExisting:n,multi:!0}]),W],ngContentSelectors:Pn,decls:15,vars:23,consts:[["checkbox",""],["input",""],["label",""],["mat-internal-form-field","",3,"click","labelPosition"],[1,"mdc-checkbox"],["aria-hidden","true",1,"mat-mdc-checkbox-touch-target",3,"click"],["type","checkbox",1,"mdc-checkbox__native-control",3,"blur","click","change","checked","indeterminate","disabled","id","required","tabIndex"],["aria-hidden","true",1,"mdc-checkbox__ripple"],["aria-hidden","true",1,"mdc-checkbox__background"],["focusable","false","viewBox","0 0 24 24",1,"mdc-checkbox__checkmark"],["fill","none","d","M1.73,12.91 8.1,19.28 22.79,4.59",1,"mdc-checkbox__checkmark-path"],[1,"mdc-checkbox__mixedmark"],["mat-ripple","","aria-hidden","true",1,"mat-mdc-checkbox-ripple","mat-focus-indicator",3,"matRippleTrigger","matRippleDisabled","matRippleCentered"],[1,"mdc-label",3,"for"]],template:function(t,i){if(t&1&&(Z(),u(0,"div",3),A("click",function(a){return i._preventBubblingFromLabel(a)}),u(1,"div",4,0)(3,"div",5),A("click",function(){return i._onTouchTargetClick()}),f(),u(4,"input",6,1),A("blur",function(){return i._onBlur()})("click",function(){return i._onInputClick()})("change",function(a){return i._onInteractionEvent(a)}),f(),q(6,"div",7),u(7,"div",8),we(),u(8,"svg",9),q(9,"path",10),f(),ft(),q(10,"div",11),f(),q(11,"div",12),f(),u(12,"label",13,2),I(14),f()()),t&2){let r=yt(2);x("labelPosition",i.labelPosition),d(4),N("mdc-checkbox--selected",i.checked),x("checked",i.checked)("indeterminate",i.indeterminate)("disabled",i.disabled&&!i.disabledInteractive)("id",i.inputId)("required",i.required)("tabIndex",i.disabled&&!i.disabledInteractive?-1:i.tabIndex),G("aria-label",i.ariaLabel||null)("aria-labelledby",i.ariaLabelledby)("aria-describedby",i.ariaDescribedby)("aria-checked",i.indeterminate?"mixed":null)("aria-controls",i.ariaControls)("aria-disabled",i.disabled&&i.disabledInteractive?!0:null)("aria-expanded",i.ariaExpanded)("aria-owns",i.ariaOwns)("name",i.name)("value",i.value),d(7),x("matRippleTrigger",r)("matRippleDisabled",i.disableRipple||i.disabled)("matRippleCentered",!0),d(),x("for",i.inputId)}},dependencies:[Tt,Et],styles:[`.mdc-checkbox {
  display: inline-block;
  position: relative;
  flex: 0 0 18px;
  box-sizing: content-box;
  width: 18px;
  height: 18px;
  line-height: 0;
  white-space: nowrap;
  cursor: pointer;
  vertical-align: bottom;
  padding: calc((var(--mat-checkbox-state-layer-size, 40px) - 18px) / 2);
  margin: calc((var(--mat-checkbox-state-layer-size, 40px) - var(--mat-checkbox-state-layer-size, 40px)) / 2);
}
.mdc-checkbox:hover > .mdc-checkbox__ripple {
  opacity: var(--mat-checkbox-unselected-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
  background-color: var(--mat-checkbox-unselected-hover-state-layer-color, var(--mat-sys-on-surface));
}
.mdc-checkbox:hover > .mat-mdc-checkbox-ripple > .mat-ripple-element {
  background-color: var(--mat-checkbox-unselected-hover-state-layer-color, var(--mat-sys-on-surface));
}
.mdc-checkbox .mdc-checkbox__native-control:focus + .mdc-checkbox__ripple {
  opacity: var(--mat-checkbox-unselected-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
  background-color: var(--mat-checkbox-unselected-focus-state-layer-color, var(--mat-sys-on-surface));
}
.mdc-checkbox .mdc-checkbox__native-control:focus ~ .mat-mdc-checkbox-ripple .mat-ripple-element {
  background-color: var(--mat-checkbox-unselected-focus-state-layer-color, var(--mat-sys-on-surface));
}
.mdc-checkbox:active > .mdc-checkbox__native-control + .mdc-checkbox__ripple {
  opacity: var(--mat-checkbox-unselected-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
  background-color: var(--mat-checkbox-unselected-pressed-state-layer-color, var(--mat-sys-primary));
}
.mdc-checkbox:active > .mdc-checkbox__native-control ~ .mat-mdc-checkbox-ripple .mat-ripple-element {
  background-color: var(--mat-checkbox-unselected-pressed-state-layer-color, var(--mat-sys-primary));
}
.mdc-checkbox:hover > .mdc-checkbox__native-control:checked + .mdc-checkbox__ripple {
  opacity: var(--mat-checkbox-selected-hover-state-layer-opacity, var(--mat-sys-hover-state-layer-opacity));
  background-color: var(--mat-checkbox-selected-hover-state-layer-color, var(--mat-sys-primary));
}
.mdc-checkbox:hover > .mdc-checkbox__native-control:checked ~ .mat-mdc-checkbox-ripple .mat-ripple-element {
  background-color: var(--mat-checkbox-selected-hover-state-layer-color, var(--mat-sys-primary));
}
.mdc-checkbox .mdc-checkbox__native-control:focus:checked + .mdc-checkbox__ripple {
  opacity: var(--mat-checkbox-selected-focus-state-layer-opacity, var(--mat-sys-focus-state-layer-opacity));
  background-color: var(--mat-checkbox-selected-focus-state-layer-color, var(--mat-sys-primary));
}
.mdc-checkbox .mdc-checkbox__native-control:focus:checked ~ .mat-mdc-checkbox-ripple .mat-ripple-element {
  background-color: var(--mat-checkbox-selected-focus-state-layer-color, var(--mat-sys-primary));
}
.mdc-checkbox:active > .mdc-checkbox__native-control:checked + .mdc-checkbox__ripple {
  opacity: var(--mat-checkbox-selected-pressed-state-layer-opacity, var(--mat-sys-pressed-state-layer-opacity));
  background-color: var(--mat-checkbox-selected-pressed-state-layer-color, var(--mat-sys-on-surface));
}
.mdc-checkbox:active > .mdc-checkbox__native-control:checked ~ .mat-mdc-checkbox-ripple .mat-ripple-element {
  background-color: var(--mat-checkbox-selected-pressed-state-layer-color, var(--mat-sys-on-surface));
}
.mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox .mdc-checkbox__native-control ~ .mat-mdc-checkbox-ripple .mat-ripple-element,
.mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox .mdc-checkbox__native-control + .mdc-checkbox__ripple {
  background-color: var(--mat-checkbox-unselected-hover-state-layer-color, var(--mat-sys-on-surface));
}
.mdc-checkbox .mdc-checkbox__native-control {
  position: absolute;
  margin: 0;
  padding: 0;
  opacity: 0;
  cursor: inherit;
  z-index: 1;
  width: var(--mat-checkbox-state-layer-size, 40px);
  height: var(--mat-checkbox-state-layer-size, 40px);
  top: calc((var(--mat-checkbox-state-layer-size, 40px) - var(--mat-checkbox-state-layer-size, 40px)) / 2);
  right: calc((var(--mat-checkbox-state-layer-size, 40px) - var(--mat-checkbox-state-layer-size, 40px)) / 2);
  left: calc((var(--mat-checkbox-state-layer-size, 40px) - var(--mat-checkbox-state-layer-size, 40px)) / 2);
}

.mdc-checkbox--disabled {
  cursor: default;
  pointer-events: none;
}

.mdc-checkbox__background {
  display: inline-flex;
  position: absolute;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  width: 18px;
  height: 18px;
  border: 2px solid currentColor;
  border-radius: 2px;
  background-color: transparent;
  pointer-events: none;
  will-change: background-color, border-color;
  transition: background-color 90ms cubic-bezier(0.4, 0, 0.6, 1), border-color 90ms cubic-bezier(0.4, 0, 0.6, 1);
  -webkit-print-color-adjust: exact;
  color-adjust: exact;
  border-color: var(--mat-checkbox-unselected-icon-color, var(--mat-sys-on-surface-variant));
  top: calc((var(--mat-checkbox-state-layer-size, 40px) - 18px) / 2);
  left: calc((var(--mat-checkbox-state-layer-size, 40px) - 18px) / 2);
}

.mdc-checkbox__native-control:enabled:checked ~ .mdc-checkbox__background,
.mdc-checkbox__native-control:enabled:indeterminate ~ .mdc-checkbox__background {
  border-color: var(--mat-checkbox-selected-icon-color, var(--mat-sys-primary));
  background-color: var(--mat-checkbox-selected-icon-color, var(--mat-sys-primary));
}

.mdc-checkbox--disabled .mdc-checkbox__background {
  border-color: var(--mat-checkbox-disabled-unselected-icon-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
}
@media (forced-colors: active) {
  .mdc-checkbox--disabled .mdc-checkbox__background {
    border-color: GrayText;
  }
}

.mdc-checkbox__native-control:disabled:checked ~ .mdc-checkbox__background,
.mdc-checkbox__native-control:disabled:indeterminate ~ .mdc-checkbox__background {
  background-color: var(--mat-checkbox-disabled-selected-icon-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
  border-color: transparent;
}
@media (forced-colors: active) {
  .mdc-checkbox__native-control:disabled:checked ~ .mdc-checkbox__background,
  .mdc-checkbox__native-control:disabled:indeterminate ~ .mdc-checkbox__background {
    border-color: GrayText;
  }
}

.mdc-checkbox:hover > .mdc-checkbox__native-control:not(:checked) ~ .mdc-checkbox__background,
.mdc-checkbox:hover > .mdc-checkbox__native-control:not(:indeterminate) ~ .mdc-checkbox__background {
  border-color: var(--mat-checkbox-unselected-hover-icon-color, var(--mat-sys-on-surface));
  background-color: transparent;
}

.mdc-checkbox:hover > .mdc-checkbox__native-control:checked ~ .mdc-checkbox__background,
.mdc-checkbox:hover > .mdc-checkbox__native-control:indeterminate ~ .mdc-checkbox__background {
  border-color: var(--mat-checkbox-selected-hover-icon-color, var(--mat-sys-primary));
  background-color: var(--mat-checkbox-selected-hover-icon-color, var(--mat-sys-primary));
}

.mdc-checkbox__native-control:focus:focus:not(:checked) ~ .mdc-checkbox__background,
.mdc-checkbox__native-control:focus:focus:not(:indeterminate) ~ .mdc-checkbox__background {
  border-color: var(--mat-checkbox-unselected-focus-icon-color, var(--mat-sys-on-surface));
}

.mdc-checkbox__native-control:focus:focus:checked ~ .mdc-checkbox__background,
.mdc-checkbox__native-control:focus:focus:indeterminate ~ .mdc-checkbox__background {
  border-color: var(--mat-checkbox-selected-focus-icon-color, var(--mat-sys-primary));
  background-color: var(--mat-checkbox-selected-focus-icon-color, var(--mat-sys-primary));
}

.mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox:hover > .mdc-checkbox__native-control ~ .mdc-checkbox__background,
.mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox .mdc-checkbox__native-control:focus ~ .mdc-checkbox__background,
.mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__background {
  border-color: var(--mat-checkbox-disabled-unselected-icon-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
}
@media (forced-colors: active) {
  .mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox:hover > .mdc-checkbox__native-control ~ .mdc-checkbox__background,
  .mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox .mdc-checkbox__native-control:focus ~ .mdc-checkbox__background,
  .mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__background {
    border-color: GrayText;
  }
}
.mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__native-control:checked ~ .mdc-checkbox__background,
.mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__native-control:indeterminate ~ .mdc-checkbox__background {
  background-color: var(--mat-checkbox-disabled-selected-icon-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
  border-color: transparent;
}

.mdc-checkbox__checkmark {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  width: 100%;
  opacity: 0;
  transition: opacity 180ms cubic-bezier(0.4, 0, 0.6, 1);
  color: var(--mat-checkbox-selected-checkmark-color, var(--mat-sys-on-primary));
}
@media (forced-colors: active) {
  .mdc-checkbox__checkmark {
    color: CanvasText;
  }
}

.mdc-checkbox--disabled .mdc-checkbox__checkmark, .mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__checkmark {
  color: var(--mat-checkbox-disabled-selected-checkmark-color, var(--mat-sys-surface));
}
@media (forced-colors: active) {
  .mdc-checkbox--disabled .mdc-checkbox__checkmark, .mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__checkmark {
    color: GrayText;
  }
}

.mdc-checkbox__checkmark-path {
  transition: stroke-dashoffset 180ms cubic-bezier(0.4, 0, 0.6, 1);
  stroke: currentColor;
  stroke-width: 3.12px;
  stroke-dashoffset: 29.7833385;
  stroke-dasharray: 29.7833385;
}

.mdc-checkbox__mixedmark {
  width: 100%;
  height: 0;
  transform: scaleX(0) rotate(0deg);
  border-width: 1px;
  border-style: solid;
  opacity: 0;
  transition: opacity 90ms cubic-bezier(0.4, 0, 0.6, 1), transform 90ms cubic-bezier(0.4, 0, 0.6, 1);
  border-color: var(--mat-checkbox-selected-checkmark-color, var(--mat-sys-on-primary));
}
@media (forced-colors: active) {
  .mdc-checkbox__mixedmark {
    margin: 0 1px;
  }
}

.mdc-checkbox--disabled .mdc-checkbox__mixedmark, .mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__mixedmark {
  border-color: var(--mat-checkbox-disabled-selected-checkmark-color, var(--mat-sys-surface));
}
@media (forced-colors: active) {
  .mdc-checkbox--disabled .mdc-checkbox__mixedmark, .mdc-checkbox--disabled.mat-mdc-checkbox-disabled-interactive .mdc-checkbox__mixedmark {
    border-color: GrayText;
  }
}

.mdc-checkbox--anim-unchecked-checked .mdc-checkbox__background,
.mdc-checkbox--anim-unchecked-indeterminate .mdc-checkbox__background,
.mdc-checkbox--anim-checked-unchecked .mdc-checkbox__background,
.mdc-checkbox--anim-indeterminate-unchecked .mdc-checkbox__background {
  animation-duration: 180ms;
  animation-timing-function: linear;
}

.mdc-checkbox--anim-unchecked-checked .mdc-checkbox__checkmark-path {
  animation: mdc-checkbox-unchecked-checked-checkmark-path 180ms linear;
  transition: none;
}

.mdc-checkbox--anim-unchecked-indeterminate .mdc-checkbox__mixedmark {
  animation: mdc-checkbox-unchecked-indeterminate-mixedmark 90ms linear;
  transition: none;
}

.mdc-checkbox--anim-checked-unchecked .mdc-checkbox__checkmark-path {
  animation: mdc-checkbox-checked-unchecked-checkmark-path 90ms linear;
  transition: none;
}

.mdc-checkbox--anim-checked-indeterminate .mdc-checkbox__checkmark {
  animation: mdc-checkbox-checked-indeterminate-checkmark 90ms linear;
  transition: none;
}
.mdc-checkbox--anim-checked-indeterminate .mdc-checkbox__mixedmark {
  animation: mdc-checkbox-checked-indeterminate-mixedmark 90ms linear;
  transition: none;
}

.mdc-checkbox--anim-indeterminate-checked .mdc-checkbox__checkmark {
  animation: mdc-checkbox-indeterminate-checked-checkmark 500ms linear;
  transition: none;
}
.mdc-checkbox--anim-indeterminate-checked .mdc-checkbox__mixedmark {
  animation: mdc-checkbox-indeterminate-checked-mixedmark 500ms linear;
  transition: none;
}

.mdc-checkbox--anim-indeterminate-unchecked .mdc-checkbox__mixedmark {
  animation: mdc-checkbox-indeterminate-unchecked-mixedmark 300ms linear;
  transition: none;
}

.mdc-checkbox__native-control:checked ~ .mdc-checkbox__background,
.mdc-checkbox__native-control:indeterminate ~ .mdc-checkbox__background {
  transition: border-color 90ms cubic-bezier(0, 0, 0.2, 1), background-color 90ms cubic-bezier(0, 0, 0.2, 1);
}
.mdc-checkbox__native-control:checked ~ .mdc-checkbox__background > .mdc-checkbox__checkmark > .mdc-checkbox__checkmark-path,
.mdc-checkbox__native-control:indeterminate ~ .mdc-checkbox__background > .mdc-checkbox__checkmark > .mdc-checkbox__checkmark-path {
  stroke-dashoffset: 0;
}

.mdc-checkbox__native-control:checked ~ .mdc-checkbox__background > .mdc-checkbox__checkmark {
  transition: opacity 180ms cubic-bezier(0, 0, 0.2, 1), transform 180ms cubic-bezier(0, 0, 0.2, 1);
  opacity: 1;
}
.mdc-checkbox__native-control:checked ~ .mdc-checkbox__background > .mdc-checkbox__mixedmark {
  transform: scaleX(1) rotate(-45deg);
}

.mdc-checkbox__native-control:indeterminate ~ .mdc-checkbox__background > .mdc-checkbox__checkmark {
  transform: rotate(45deg);
  opacity: 0;
  transition: opacity 90ms cubic-bezier(0.4, 0, 0.6, 1), transform 90ms cubic-bezier(0.4, 0, 0.6, 1);
}
.mdc-checkbox__native-control:indeterminate ~ .mdc-checkbox__background > .mdc-checkbox__mixedmark {
  transform: scaleX(1) rotate(0deg);
  opacity: 1;
}

@keyframes mdc-checkbox-unchecked-checked-checkmark-path {
  0%, 50% {
    stroke-dashoffset: 29.7833385;
  }
  50% {
    animation-timing-function: cubic-bezier(0, 0, 0.2, 1);
  }
  100% {
    stroke-dashoffset: 0;
  }
}
@keyframes mdc-checkbox-unchecked-indeterminate-mixedmark {
  0%, 68.2% {
    transform: scaleX(0);
  }
  68.2% {
    animation-timing-function: cubic-bezier(0, 0, 0, 1);
  }
  100% {
    transform: scaleX(1);
  }
}
@keyframes mdc-checkbox-checked-unchecked-checkmark-path {
  from {
    animation-timing-function: cubic-bezier(0.4, 0, 1, 1);
    opacity: 1;
    stroke-dashoffset: 0;
  }
  to {
    opacity: 0;
    stroke-dashoffset: -29.7833385;
  }
}
@keyframes mdc-checkbox-checked-indeterminate-checkmark {
  from {
    animation-timing-function: cubic-bezier(0, 0, 0.2, 1);
    transform: rotate(0deg);
    opacity: 1;
  }
  to {
    transform: rotate(45deg);
    opacity: 0;
  }
}
@keyframes mdc-checkbox-indeterminate-checked-checkmark {
  from {
    animation-timing-function: cubic-bezier(0.14, 0, 0, 1);
    transform: rotate(45deg);
    opacity: 0;
  }
  to {
    transform: rotate(360deg);
    opacity: 1;
  }
}
@keyframes mdc-checkbox-checked-indeterminate-mixedmark {
  from {
    animation-timing-function: cubic-bezier(0, 0, 0.2, 1);
    transform: rotate(-45deg);
    opacity: 0;
  }
  to {
    transform: rotate(0deg);
    opacity: 1;
  }
}
@keyframes mdc-checkbox-indeterminate-checked-mixedmark {
  from {
    animation-timing-function: cubic-bezier(0.14, 0, 0, 1);
    transform: rotate(0deg);
    opacity: 1;
  }
  to {
    transform: rotate(315deg);
    opacity: 0;
  }
}
@keyframes mdc-checkbox-indeterminate-unchecked-mixedmark {
  0% {
    animation-timing-function: linear;
    transform: scaleX(1);
    opacity: 1;
  }
  32.8%, 100% {
    transform: scaleX(0);
    opacity: 0;
  }
}
.mat-mdc-checkbox {
  display: inline-block;
  position: relative;
  -webkit-tap-highlight-color: transparent;
}
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mat-mdc-checkbox-touch-target,
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mdc-checkbox__native-control,
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mdc-checkbox__ripple,
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mat-mdc-checkbox-ripple::before,
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mdc-checkbox__background,
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mdc-checkbox__background > .mdc-checkbox__checkmark,
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mdc-checkbox__background > .mdc-checkbox__checkmark > .mdc-checkbox__checkmark-path,
.mat-mdc-checkbox._mat-animation-noopable > .mat-internal-form-field > .mdc-checkbox > .mdc-checkbox__background > .mdc-checkbox__mixedmark {
  transition: none !important;
  animation: none !important;
}
.mat-mdc-checkbox label {
  cursor: pointer;
}
.mat-mdc-checkbox .mat-internal-form-field {
  color: var(--mat-checkbox-label-text-color, var(--mat-sys-on-surface));
  font-family: var(--mat-checkbox-label-text-font, var(--mat-sys-body-medium-font));
  line-height: var(--mat-checkbox-label-text-line-height, var(--mat-sys-body-medium-line-height));
  font-size: var(--mat-checkbox-label-text-size, var(--mat-sys-body-medium-size));
  letter-spacing: var(--mat-checkbox-label-text-tracking, var(--mat-sys-body-medium-tracking));
  font-weight: var(--mat-checkbox-label-text-weight, var(--mat-sys-body-medium-weight));
}
.mat-mdc-checkbox.mat-mdc-checkbox-disabled.mat-mdc-checkbox-disabled-interactive {
  pointer-events: auto;
}
.mat-mdc-checkbox.mat-mdc-checkbox-disabled.mat-mdc-checkbox-disabled-interactive input {
  cursor: default;
}
.mat-mdc-checkbox.mat-mdc-checkbox-disabled label {
  cursor: default;
  color: var(--mat-checkbox-disabled-label-color, color-mix(in srgb, var(--mat-sys-on-surface) 38%, transparent));
}
@media (forced-colors: active) {
  .mat-mdc-checkbox.mat-mdc-checkbox-disabled label {
    color: GrayText;
  }
}
.mat-mdc-checkbox label:empty {
  display: none;
}
.mat-mdc-checkbox .mdc-checkbox__ripple {
  opacity: 0;
}

.mat-mdc-checkbox .mat-mdc-checkbox-ripple,
.mdc-checkbox__ripple {
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  position: absolute;
  border-radius: 50%;
  pointer-events: none;
}
.mat-mdc-checkbox .mat-mdc-checkbox-ripple:not(:empty),
.mdc-checkbox__ripple:not(:empty) {
  transform: translateZ(0);
}

.mat-mdc-checkbox-ripple .mat-ripple-element {
  opacity: 0.1;
}

.mat-mdc-checkbox-touch-target {
  position: absolute;
  top: 50%;
  left: 50%;
  height: var(--mat-checkbox-touch-target-size, 48px);
  width: var(--mat-checkbox-touch-target-size, 48px);
  transform: translate(-50%, -50%);
  display: var(--mat-checkbox-touch-target-display, block);
}

.mat-mdc-checkbox .mat-mdc-checkbox-ripple::before {
  border-radius: 50%;
}

.mdc-checkbox__native-control:focus-visible ~ .mat-focus-indicator::before {
  content: "";
}
`],encapsulation:2,changeDetection:0})}return n})(),mn=(()=>{class n{static \u0275fac=function(t){return new(t||n)};static \u0275mod=U({type:n});static \u0275inj=L({imports:[tt,ee]})}return n})();function Ln(n,o){n&1&&ie(0,"div",2)}var Un=new j("MAT_PROGRESS_BAR_DEFAULT_OPTIONS");var un=(()=>{class n{_elementRef=s(S);_ngZone=s(xe);_changeDetectorRef=s(J);_renderer=s(_t);_cleanupTransitionEnd;constructor(){let e=xt(),t=s(Un,{optional:!0});this._isNoopAnimation=e==="di-disabled",e==="reduced-motion"&&this._elementRef.nativeElement.classList.add("mat-progress-bar-reduced-motion"),t&&(t.color&&(this.color=this._defaultColor=t.color),this.mode=t.mode||this.mode)}_isNoopAnimation;get color(){return this._color||this._defaultColor}set color(e){this._color=e}_color;_defaultColor="primary";get value(){return this._value}set value(e){this._value=hn(e||0),this._changeDetectorRef.markForCheck()}_value=0;get bufferValue(){return this._bufferValue||0}set bufferValue(e){this._bufferValue=hn(e||0),this._changeDetectorRef.markForCheck()}_bufferValue=0;animationEnd=new X;get mode(){return this._mode}set mode(e){this._mode=e,this._changeDetectorRef.markForCheck()}_mode="determinate";ngAfterViewInit(){this._ngZone.runOutsideAngular(()=>{this._cleanupTransitionEnd=this._renderer.listen(this._elementRef.nativeElement,"transitionend",this._transitionendHandler)})}ngOnDestroy(){this._cleanupTransitionEnd?.()}_getPrimaryBarTransform(){return`scaleX(${this._isIndeterminate()?1:this.value/100})`}_getBufferBarFlexBasis(){return`${this.mode==="buffer"?this.bufferValue:100}%`}_isIndeterminate(){return this.mode==="indeterminate"||this.mode==="query"}_transitionendHandler=e=>{this.animationEnd.observers.length===0||!e.target||!e.target.classList.contains("mdc-linear-progress__primary-bar")||(this.mode==="determinate"||this.mode==="buffer")&&this._ngZone.run(()=>this.animationEnd.next({value:this.value}))};static \u0275fac=function(t){return new(t||n)};static \u0275cmp=v({type:n,selectors:[["mat-progress-bar"]],hostAttrs:["role","progressbar","aria-valuemin","0","aria-valuemax","100","tabindex","-1",1,"mat-mdc-progress-bar","mdc-linear-progress"],hostVars:10,hostBindings:function(t,i){t&2&&(G("aria-valuenow",i._isIndeterminate()?null:i.value)("mode",i.mode),Se("mat-"+i.color),N("_mat-animation-noopable",i._isNoopAnimation)("mdc-linear-progress--animation-ready",!i._isNoopAnimation)("mdc-linear-progress--indeterminate",i._isIndeterminate()))},inputs:{color:"color",value:[2,"value","value",ge],bufferValue:[2,"bufferValue","bufferValue",ge],mode:"mode"},outputs:{animationEnd:"animationEnd"},exportAs:["matProgressBar"],decls:7,vars:5,consts:[["aria-hidden","true",1,"mdc-linear-progress__buffer"],[1,"mdc-linear-progress__buffer-bar"],[1,"mdc-linear-progress__buffer-dots"],["aria-hidden","true",1,"mdc-linear-progress__bar","mdc-linear-progress__primary-bar"],[1,"mdc-linear-progress__bar-inner"],["aria-hidden","true",1,"mdc-linear-progress__bar","mdc-linear-progress__secondary-bar"]],template:function(t,i){t&1&&(K(0,"div",0),ie(1,"div",1),C(2,Ln,1,0,"div",2),$(),K(3,"div",3),ie(4,"span",4),$(),K(5,"div",5),ie(6,"span",4),$()),t&2&&(d(),Y("flex-basis",i._getBufferBarFlexBasis()),d(),w(i.mode==="buffer"?2:-1),d(),Y("transform",i._getPrimaryBarTransform()))},styles:[`.mat-mdc-progress-bar {
  --mat-progress-bar-animation-multiplier: 1;
  display: block;
  text-align: start;
}
.mat-mdc-progress-bar[mode=query] {
  transform: scaleX(-1);
}
.mat-mdc-progress-bar._mat-animation-noopable .mdc-linear-progress__buffer-dots,
.mat-mdc-progress-bar._mat-animation-noopable .mdc-linear-progress__primary-bar,
.mat-mdc-progress-bar._mat-animation-noopable .mdc-linear-progress__secondary-bar,
.mat-mdc-progress-bar._mat-animation-noopable .mdc-linear-progress__bar-inner.mdc-linear-progress__bar-inner {
  animation: none;
}
.mat-mdc-progress-bar._mat-animation-noopable .mdc-linear-progress__primary-bar,
.mat-mdc-progress-bar._mat-animation-noopable .mdc-linear-progress__buffer-bar {
  transition: transform 1ms;
}

.mat-progress-bar-reduced-motion {
  --mat-progress-bar-animation-multiplier: 2;
}

.mdc-linear-progress {
  position: relative;
  width: 100%;
  transform: translateZ(0);
  outline: 1px solid transparent;
  overflow-x: hidden;
  transition: opacity 250ms 0ms cubic-bezier(0.4, 0, 0.6, 1);
  height: max(var(--mat-progress-bar-track-height, 4px), var(--mat-progress-bar-active-indicator-height, 4px));
}
@media (forced-colors: active) {
  .mdc-linear-progress {
    outline-color: CanvasText;
  }
}

.mdc-linear-progress__bar {
  position: absolute;
  top: 0;
  bottom: 0;
  margin: auto 0;
  width: 100%;
  animation: none;
  transform-origin: top left;
  transition: transform 250ms 0ms cubic-bezier(0.4, 0, 0.6, 1);
  height: var(--mat-progress-bar-active-indicator-height, 4px);
}
.mdc-linear-progress--indeterminate .mdc-linear-progress__bar {
  transition: none;
}
[dir=rtl] .mdc-linear-progress__bar {
  right: 0;
  transform-origin: center right;
}

.mdc-linear-progress__bar-inner {
  display: inline-block;
  position: absolute;
  width: 100%;
  animation: none;
  border-top-style: solid;
  border-color: var(--mat-progress-bar-active-indicator-color, var(--mat-sys-primary));
  border-top-width: var(--mat-progress-bar-active-indicator-height, 4px);
}

.mdc-linear-progress__buffer {
  display: flex;
  position: absolute;
  top: 0;
  bottom: 0;
  margin: auto 0;
  width: 100%;
  overflow: hidden;
  height: var(--mat-progress-bar-track-height, 4px);
  border-radius: var(--mat-progress-bar-track-shape, var(--mat-sys-corner-none));
}

.mdc-linear-progress__buffer-dots {
  background-image: radial-gradient(circle, var(--mat-progress-bar-track-color, var(--mat-sys-surface-variant)) calc(var(--mat-progress-bar-track-height, 4px) / 2), transparent 0);
  background-repeat: repeat-x;
  background-size: calc(calc(var(--mat-progress-bar-track-height, 4px) / 2) * 5);
  background-position: left;
  flex: auto;
  transform: rotate(180deg);
  animation: mdc-linear-progress-buffering calc(250ms * var(--mat-progress-bar-animation-multiplier)) infinite linear;
}
@media (forced-colors: active) {
  .mdc-linear-progress__buffer-dots {
    background-color: ButtonBorder;
  }
}
[dir=rtl] .mdc-linear-progress__buffer-dots {
  animation: mdc-linear-progress-buffering-reverse calc(250ms * var(--mat-progress-bar-animation-multiplier)) infinite linear;
  transform: rotate(0);
}

.mdc-linear-progress__buffer-bar {
  flex: 0 1 100%;
  transition: flex-basis 250ms 0ms cubic-bezier(0.4, 0, 0.6, 1);
  background-color: var(--mat-progress-bar-track-color, var(--mat-sys-surface-variant));
}

.mdc-linear-progress__primary-bar {
  transform: scaleX(0);
}
.mdc-linear-progress--indeterminate .mdc-linear-progress__primary-bar {
  left: -145.166611%;
}
.mdc-linear-progress--indeterminate.mdc-linear-progress--animation-ready .mdc-linear-progress__primary-bar {
  animation: mdc-linear-progress-primary-indeterminate-translate calc(2s * var(--mat-progress-bar-animation-multiplier)) infinite linear;
}
.mdc-linear-progress--indeterminate.mdc-linear-progress--animation-ready .mdc-linear-progress__primary-bar > .mdc-linear-progress__bar-inner {
  animation: mdc-linear-progress-primary-indeterminate-scale calc(2s * var(--mat-progress-bar-animation-multiplier)) infinite linear;
}
[dir=rtl] .mdc-linear-progress.mdc-linear-progress--animation-ready .mdc-linear-progress__primary-bar {
  animation-name: mdc-linear-progress-primary-indeterminate-translate-reverse;
}
[dir=rtl] .mdc-linear-progress.mdc-linear-progress--indeterminate .mdc-linear-progress__primary-bar {
  right: -145.166611%;
  left: auto;
}

.mdc-linear-progress__secondary-bar {
  display: none;
}
.mdc-linear-progress--indeterminate .mdc-linear-progress__secondary-bar {
  left: -54.888891%;
  display: block;
}
.mdc-linear-progress--indeterminate.mdc-linear-progress--animation-ready .mdc-linear-progress__secondary-bar {
  animation: mdc-linear-progress-secondary-indeterminate-translate calc(2s * var(--mat-progress-bar-animation-multiplier)) infinite linear;
}
.mdc-linear-progress--indeterminate.mdc-linear-progress--animation-ready .mdc-linear-progress__secondary-bar > .mdc-linear-progress__bar-inner {
  animation: mdc-linear-progress-secondary-indeterminate-scale calc(2s * var(--mat-progress-bar-animation-multiplier)) infinite linear;
}
[dir=rtl] .mdc-linear-progress.mdc-linear-progress--animation-ready .mdc-linear-progress__secondary-bar {
  animation-name: mdc-linear-progress-secondary-indeterminate-translate-reverse;
}
[dir=rtl] .mdc-linear-progress.mdc-linear-progress--indeterminate .mdc-linear-progress__secondary-bar {
  right: -54.888891%;
  left: auto;
}

@keyframes mdc-linear-progress-buffering {
  from {
    transform: rotate(180deg) translateX(calc(var(--mat-progress-bar-track-height, 4px) * -2.5));
  }
}
@keyframes mdc-linear-progress-primary-indeterminate-translate {
  0% {
    transform: translateX(0);
  }
  20% {
    animation-timing-function: cubic-bezier(0.5, 0, 0.701732, 0.495819);
    transform: translateX(0);
  }
  59.15% {
    animation-timing-function: cubic-bezier(0.302435, 0.381352, 0.55, 0.956352);
    transform: translateX(83.67142%);
  }
  100% {
    transform: translateX(200.611057%);
  }
}
@keyframes mdc-linear-progress-primary-indeterminate-scale {
  0% {
    transform: scaleX(0.08);
  }
  36.65% {
    animation-timing-function: cubic-bezier(0.334731, 0.12482, 0.785844, 1);
    transform: scaleX(0.08);
  }
  69.15% {
    animation-timing-function: cubic-bezier(0.06, 0.11, 0.6, 1);
    transform: scaleX(0.661479);
  }
  100% {
    transform: scaleX(0.08);
  }
}
@keyframes mdc-linear-progress-secondary-indeterminate-translate {
  0% {
    animation-timing-function: cubic-bezier(0.15, 0, 0.515058, 0.409685);
    transform: translateX(0);
  }
  25% {
    animation-timing-function: cubic-bezier(0.31033, 0.284058, 0.8, 0.733712);
    transform: translateX(37.651913%);
  }
  48.35% {
    animation-timing-function: cubic-bezier(0.4, 0.627035, 0.6, 0.902026);
    transform: translateX(84.386165%);
  }
  100% {
    transform: translateX(160.277782%);
  }
}
@keyframes mdc-linear-progress-secondary-indeterminate-scale {
  0% {
    animation-timing-function: cubic-bezier(0.205028, 0.057051, 0.57661, 0.453971);
    transform: scaleX(0.08);
  }
  19.15% {
    animation-timing-function: cubic-bezier(0.152313, 0.196432, 0.648374, 1.004315);
    transform: scaleX(0.457104);
  }
  44.15% {
    animation-timing-function: cubic-bezier(0.257759, -0.003163, 0.211762, 1.38179);
    transform: scaleX(0.72796);
  }
  100% {
    transform: scaleX(0.08);
  }
}
@keyframes mdc-linear-progress-primary-indeterminate-translate-reverse {
  0% {
    transform: translateX(0);
  }
  20% {
    animation-timing-function: cubic-bezier(0.5, 0, 0.701732, 0.495819);
    transform: translateX(0);
  }
  59.15% {
    animation-timing-function: cubic-bezier(0.302435, 0.381352, 0.55, 0.956352);
    transform: translateX(-83.67142%);
  }
  100% {
    transform: translateX(-200.611057%);
  }
}
@keyframes mdc-linear-progress-secondary-indeterminate-translate-reverse {
  0% {
    animation-timing-function: cubic-bezier(0.15, 0, 0.515058, 0.409685);
    transform: translateX(0);
  }
  25% {
    animation-timing-function: cubic-bezier(0.31033, 0.284058, 0.8, 0.733712);
    transform: translateX(-37.651913%);
  }
  48.35% {
    animation-timing-function: cubic-bezier(0.4, 0.627035, 0.6, 0.902026);
    transform: translateX(-84.386165%);
  }
  100% {
    transform: translateX(-160.277782%);
  }
}
@keyframes mdc-linear-progress-buffering-reverse {
  from {
    transform: translateX(-10px);
  }
}
`],encapsulation:2,changeDetection:0})}return n})();function hn(n,o=0,e=100){return Math.max(o,Math.min(e,n))}var fn=(()=>{class n{static \u0275fac=function(t){return new(t||n)};static \u0275mod=U({type:n});static \u0275inj=L({imports:[ee]})}return n})();var jn=(n,o)=>({$implicit:n,value:o}),Xn=(n,o)=>o.key;function qn(n,o){n&1&&q(0,"mat-progress-bar",1)}function $n(n,o){if(n&1){let e=ue();u(0,"th",12)(1,"mat-checkbox",13),A("change",function(){ce(e);let i=b(2);return le(i.toggleAllRows())}),f()()}if(n&2){let e=b(2);d(),x("checked",e.isAllSelected())("indeterminate",e.selection.hasValue()&&!e.isAllSelected())}}function Qn(n,o){if(n&1){let e=ue();u(0,"td",14)(1,"mat-checkbox",15),A("change",function(){let i=ce(e).$implicit,r=b(2);return le(r.toggleRow(i))})("click",function(i){return i.stopPropagation()}),f()()}if(n&2){let e=o.$implicit,t=b(2);d(),x("checked",t.selection.isSelected(e))}}function Wn(n,o){n&1&&(me(0,4),ae(1,$n,2,2,"th",10)(2,Qn,2,1,"td",11),he())}function Gn(n,o){if(n&1&&(u(0,"th",18),pe(1),f()),n&2){let e=b().$implicit;Y("width",e.width??"auto")("text-align",e.align??"left"),x("mat-sort-header",e.sortable?e.key:"")("disabled",!e.sortable),d(),_e(" ",e.header," ")}}function Kn(n,o){if(n&1&&D(0,19),n&2){let e=b().$implicit,t=b().$implicit,i=b();x("ngTemplateOutlet",o)("ngTemplateOutletContext",vt(2,jn,e,i.getCellValue(e,t)))}}function Zn(n,o){if(n&1&&pe(0),n&2){let e=b().$implicit,t=b().$implicit,i=b();_e(" ",i.getCellValue(e,t)," ")}}function Yn(n,o){if(n&1&&(u(0,"td",14),C(1,Kn,1,5,"ng-container",19)(2,Zn,1,1),f()),n&2){let e,t=b().$implicit,i=b();Y("text-align",t.align??"left"),d(),w((e=i.getCellTemplate(t.key))?1:2,e)}}function Jn(n,o){if(n&1&&(me(0,5),ae(1,Gn,2,7,"th",16)(2,Yn,3,3,"td",17),he()),n&2){let e=o.$implicit;x("matColumnDef",e.key)}}function ei(n,o){n&1&&q(0,"tr",20)}function ti(n,o){if(n&1){let e=ue();u(0,"tr",21),A("click",function(){let i=ce(e).$implicit,r=b();return le(r.rowClick.emit(i))}),f()}}function ni(n,o){if(n&1&&q(0,"ui-empty-state",8),n&2){let e=b();x("title",e.emptyMessage())}}function ii(n,o){if(n&1){let e=ue();u(0,"mat-paginator",22),A("page",function(i){ce(e);let r=b();return le(r.onPageChange(i))}),f()}if(n&2){let e=b();x("length",e.pagination().total)("pageIndex",e.pagination().page-1)("pageSize",e.pagination().pageSize)("pageSizeOptions",e.pageSizeOptions())}}var nt=class n{constructor(o){this.templateRef=o}columnKey=V.required({alias:"uiCellDef"});static \u0275fac=function(e){return new(e||n)(Re(B))};static \u0275dir=m({type:n,selectors:[["","uiCellDef",""]],inputs:{columnKey:[1,"uiCellDef","columnKey"]}})},pn=class n{columns=V.required();data=V.required();pagination=V(null);sort=V(null);loading=V(!1);emptyMessage=V("No se encontraron registros.");selectable=V(!1);pageSizeOptions=V([10,25,50,100]);pageChange=be();sortChange=be();rowClick=be();selectionChange=be();cellDefs;selection=new Ot(!0,[]);displayedColumns=Ue(()=>{let o=this.columns().map(e=>e.key);return this.selectable()?["select",...o]:o});sortDirection=Ue(()=>{let o=this.sort()?.sortDir;return o==="asc"||o==="desc"?o:""});getCellTemplate(o){return this.cellDefs?.find(e=>e.columnKey()===o)?.templateRef??null}getCellValue(o,e){return e.valueAccessor?e.valueAccessor(o):o[e.key]??""}isAllSelected(){return this.selection.selected.length===this.data().length&&this.data().length>0}toggleAllRows(){this.isAllSelected()?this.selection.clear():this.selection.select(...this.data()),this.selectionChange.emit(this.selection.selected)}toggleRow(o){this.selection.toggle(o),this.selectionChange.emit(this.selection.selected)}onSortChange(o){this.sortChange.emit({sortBy:o.active,sortDir:o.direction==="desc"?"desc":"asc"})}onPageChange(o){this.pageChange.emit({page:o.pageIndex+1,pageSize:o.pageSize})}static \u0275fac=function(e){return new(e||n)};static \u0275cmp=v({type:n,selectors:[["ui-data-table"]],contentQueries:function(e,t,i){if(e&1&&fe(i,nt,4),e&2){let r;T(r=M())&&(t.cellDefs=r)}},inputs:{columns:[1,"columns"],data:[1,"data"],pagination:[1,"pagination"],sort:[1,"sort"],loading:[1,"loading"],emptyMessage:[1,"emptyMessage"],selectable:[1,"selectable"],pageSizeOptions:[1,"pageSizeOptions"]},outputs:{pageChange:"pageChange",sortChange:"sortChange",rowClick:"rowClick",selectionChange:"selectionChange"},decls:11,vars:9,consts:[[1,"table-card"],["mode","indeterminate",1,"table-progress"],[1,"table-scroll"],["mat-table","","matSort","",1,"w-full",3,"matSortChange","dataSource","matSortActive","matSortDirection"],["matColumnDef","select"],[3,"matColumnDef"],["mat-header-row","",4,"matHeaderRowDef"],["mat-row","","class","cursor-pointer",3,"click",4,"matRowDef","matRowDefColumns"],[3,"title"],["showFirstLastButtons","","aria-label","Paginaci\xF3n",3,"length","pageIndex","pageSize","pageSizeOptions"],["mat-header-cell","","class","w-12",4,"matHeaderCellDef"],["mat-cell","",4,"matCellDef"],["mat-header-cell","",1,"w-12"],["aria-label","Select all rows",3,"change","checked","indeterminate"],["mat-cell",""],["aria-label","Select row",3,"change","click","checked"],["mat-header-cell","",3,"mat-sort-header","disabled","width","text-align",4,"matHeaderCellDef"],["mat-cell","",3,"text-align",4,"matCellDef"],["mat-header-cell","",3,"mat-sort-header","disabled"],[3,"ngTemplateOutlet","ngTemplateOutletContext"],["mat-header-row",""],["mat-row","",1,"cursor-pointer",3,"click"],["showFirstLastButtons","","aria-label","Paginaci\xF3n",3,"page","length","pageIndex","pageSize","pageSizeOptions"]],template:function(e,t){if(e&1&&(u(0,"div",0),C(1,qn,1,0,"mat-progress-bar",1),u(2,"div",2)(3,"table",3),A("matSortChange",function(r){return t.onSortChange(r)}),C(4,Wn,3,0,"ng-container",4),bt(5,Jn,3,1,"ng-container",5,Xn),ae(7,ei,1,0,"tr",6)(8,ti,1,0,"tr",7),f()(),C(9,ni,1,1,"ui-empty-state",8),C(10,ii,1,4,"mat-paginator",9),f()),e&2){let i;d(),w(t.loading()?1:-1),d(2),x("dataSource",t.data())("matSortActive",((i=t.sort())==null?null:i.sortBy)??"")("matSortDirection",t.sortDirection()),d(),w(t.selectable()?4:-1),d(),gt(t.columns()),d(2),x("matHeaderRowDef",t.displayedColumns()),d(),x("matRowDefColumns",t.displayedColumns()),d(),w(!t.loading()&&t.data().length===0?9:-1),d(),w(t.pagination()?10:-1)}},dependencies:[Ct,sn,Kt,Yt,nn,Jt,Zt,rn,en,tn,on,an,dn,Ye,ln,Ut,Lt,mn,tt,fn,un,Vt],styles:["[_nghost-%COMP%]{display:block}.table-card[_ngcontent-%COMP%]{position:relative;background:var(--eg-surface);border:1px solid var(--eg-border-subtle);border-radius:var(--eg-radius-lg);box-shadow:var(--eg-shadow-xs);overflow:hidden}.table-progress[_ngcontent-%COMP%]{position:absolute;top:0;left:0;right:0;z-index:10}.table-scroll[_ngcontent-%COMP%]{overflow-x:auto}table[_ngcontent-%COMP%]{border-collapse:separate;border-spacing:0}th.mat-mdc-header-cell[_ngcontent-%COMP%]{background:var(--eg-surface-dim);border-bottom:1px solid var(--eg-border);font-weight:600;font-size:.6875rem;text-transform:uppercase;letter-spacing:.05em;color:var(--eg-text-muted);padding:0 16px;height:40px;white-space:nowrap}td.mat-mdc-cell[_ngcontent-%COMP%]{border-bottom:1px solid var(--eg-border-subtle);padding:0 16px;height:48px;color:var(--eg-text-primary);font-size:.875rem}tr.mat-mdc-row[_ngcontent-%COMP%]{transition:background-color var(--eg-transition-fast)}tr.mat-mdc-row[_ngcontent-%COMP%]:hover{background:var(--eg-surface-container)!important}tr.mat-mdc-row[_ngcontent-%COMP%]:last-child   td.mat-mdc-cell[_ngcontent-%COMP%]{border-bottom:none}"],changeDetection:0})};export{nt as a,pn as b};
